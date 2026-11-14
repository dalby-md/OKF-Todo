const fs = require('node:fs/promises');
const path = require('node:path');
const { chromium } = require('playwright');

const run = path.resolve(__dirname, '../../artifacts/promotion-demo');
const capture = path.join(run, 'capture');
const tasks = {
  task1: ['Investigate invoice export timeout (DEMO-17)', 'Check available export logs for timeout details.'],
  task2: ['Prepare release checklist', 'Define rollback steps.'],
};

async function recordScene(browser, origin, scene) {
  const size = { width: 1440, height: 1000 };
  const context = await browser.newContext({
    viewport: size, recordVideo: { dir: path.join(capture, 'clips'), size },
  });
  let video;
  let start;
  let opened;
  try {
    await context.addInitScript(() => {
      const listeners = [];
      window.chrome ??= {};
      window.chrome.webview = {
        addEventListener(type, listener) {
          if (type === 'message') listeners.push(listener);
        },
        async postMessage(message) {
          const response = await fetch('/__ui-test/bridge', {
            method: 'POST', headers: { 'Content-Type': 'application/json' }, body: message,
          });
          const data = await response.text();
          for (const listener of listeners) listener({ data });
        },
      };
    });
    await context.grantPermissions(['clipboard-read', 'clipboard-write'], { origin });
    const page = await context.newPage();
    opened = performance.now();
    video = page.video();
    await page.goto(origin + '/recording/scenes.html');
    await page.evaluate(s => window.showScene(s.scene, s.step, s.title, s.subtitle, s.caption), scene);
    const app = page.frameLocator('#app');
    if (scene.action) {
      await app.getByRole('button', { name: 'Help', exact: true }).waitFor();
      const skip = app.getByRole('button', { name: 'Skip', exact: true });
      if (await skip.isVisible()) await skip.click();
    }

    if (scene.action === 'help') {
      await app.getByRole('button', { name: 'Help', exact: true }).click();
      await app.getByRole('button', { name: /OKF layer/ }).click();
      const copy = app.getByRole('button', { name: 'Copy prompt', exact: true });
      await copy.scrollIntoViewIfNeeded();
      start = performance.now();
      await page.waitForTimeout(4000);
      await copy.click();
      const clipboard = await page.evaluate(() => navigator.clipboard.readText());
      const databasePath = path.join(run, 'data', 'promotion-demo.db').replaceAll('\\', '/');
      if (!clipboard.replaceAll('\\', '/').includes(databasePath) || clipboard.includes('{{')) {
        throw new Error('Help did not copy the isolated demonstration database path.');
      }
    } else if (scene.action) {
      const [title, check] = tasks[scene.action];
      await app.getByRole('button', { name: new RegExp('^' + title.split(' (')[0]) })
        .filter({ hasText: title }).click();
      const checkbox = app.getByRole('checkbox', { name: 'Complete ' + check, exact: true });
      await checkbox.waitFor();
      const dismiss = app.getByRole('button', { name: 'Dismiss selection tip', exact: true });
      if (await dismiss.isVisible()) await dismiss.click();
      await checkbox.scrollIntoViewIfNeeded();
      const taskTitle = app.getByRole('textbox', { name: 'Task title', exact: true });
      if (await taskTitle.inputValue() !== title) throw new Error('Captured the wrong task.');
      start = performance.now();
    } else {
      start = performance.now();
    }

    await page.waitForTimeout(Math.max(0, scene.seconds * 1000 - (performance.now() - start)));
    await page.screenshot({ path: path.join(capture, scene.name + '.png') });
  } finally {
    await context.close();
  }
  return {
    name: scene.name, path: path.relative(capture, await video.path()),
    start: (start - opened) / 1000, duration: scene.seconds,
  };
}

async function main() {
  const origin = (await fs.readFile(path.join(run, 'browser-url.txt'), 'utf8')).trim();
  if (!/^http:\/\/127\.0\.0\.1:\d+$/.test(origin)) throw new Error('Expected a loopback demo host.');
  const scenes = JSON.parse(await fs.readFile(path.join(__dirname, 'batch.json'), 'utf8'));
  const executablePath = process.env.PROMOTION_CHROMIUM;
  const browser = await chromium.launch({ executablePath });
  try {
    const clips = [];
    for (const scene of scenes) {
      console.log(scene.name);
      clips.push(await recordScene(browser, origin, scene));
    }
    await fs.writeFile(path.join(capture, 'clips.json'), JSON.stringify(clips, null, 2) + '\n');
  } finally {
    await browser.close();
  }
}

main().catch(error => {
  console.error(error);
  process.exitCode = 1;
});
