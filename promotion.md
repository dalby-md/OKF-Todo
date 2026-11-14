# OKF Todo promotion plan

Last updated 2026-11-14.

OKF Todo has been shared on LinkedIn and Reddit, listed on AlternativeTo, introduced on DEV Community, and launched on Product Hunt. Søren has sent the Console.dev pitch for editorial review.

## Message and material

Lead with the app's everyday use. **Keep each investigation and its follow-up in one local workspace.** A developer can create a task for a reported bug, attach the error screenshot, add a checklist, and use the Timeline to see what has changed when they return to it.

Use optional AI assistance as the second point. A compatible assistant can turn incident notes into a proposed investigation checklist. The developer reviews it and approves what the assistant saves through the included MCP server, then continues in the desktop app. The built-in Help explains how to connect an assistant.

Assistant capabilities, accounts, costs, and data handling depend on the chosen service. Windows has packaged installers. macOS and Linux can run from source. The current repository uses the [MIT License](LICENSE).

Give readers a direct way to evaluate the app. Show one screenshot of the workspace with a task and its checklist. Link to the project page for the overview and Windows install options, or to GitHub for source and setup. Use the written example when explaining the AI workflow. Keep the video available as optional supporting material.

- [Project page](https://dalby.md/projects/okf-todo)
- [Screenshot tour](https://dalby.md/projects/okf-todo/screenshots)
- [GitHub repository](https://github.com/dalby-md/OKF-Todo)
- [Written walkthrough](docs/source-to-plan.md) and [sample files](demonstratio-kit/README.md)
- [Optional 83-second video](https://youtu.be/cXIRZgOagrA)

When sharing the video, disclose that it uses fictional input, replayed planning and approval, and application captures from a browser test host.

## Console.dev pitch

Pitch OKF Todo for **Interesting tools**. Open with the work a developer needs to track after a bug report or support handoff. Mention the task, evidence, checklist, and history they can keep together locally. Give one sentence to using MCP with their own assistant. Link directly to the project page for the workspace screenshot and Windows installation, and to GitHub for the source and setup instructions. [Console.dev criteria](https://console.dev/selection-criteria#submit-a-tool)

## Activity

| Channel | Status |
| --- | --- |
| dalby.md | Verified live in Chrome on 2026-11-14. The project page opens with the bug investigation example, puts both Windows install links and the failed deployment checklist screenshot near the top, and places the video under **Optional AI walkthrough**. Google and Bing accepted the project-page submissions and processed the sitemap on 2026-09-27. Indexing was unconfirmed in that check. Search maintenance lives in `C:\git\dalbyweb\search_machine.md`. [Project page](https://dalby.md/projects/okf-todo) |
| GitHub README | Verified live in Chrome on 2026-11-14. The README opens with the bug investigation example, shows the Windows install links and the failed deployment checklist screenshot, and links to the video from **Optional AI walkthrough**. [GitHub README](https://github.com/dalby-md/OKF-Todo#readme) |
| AlternativeTo | Listed since 2026-09-27. The page has screenshots, the video, Microsoft Store and GitHub links, and four related alternatives as of 2026-11-13. The corrections were submitted on 2026-11-13. Verified on 2026-11-14 that the public listing displays **MIT** and that the Features list omits **Reminders** and **Support for @mentions**. [Listing](https://alternativeto.net/software/okf-todo/about/) |
| LinkedIn | On hold as of 2026-11-14. Søren published a personal introduction about building the app with Codex and discarded the follow-up draft. [Original post](https://www.linkedin.com/posts/sdalby_codex-activity-7493181791499907072-Xhwk) |
| DEV Community | The introduction went live on 2026-10-05 and was corrected to MIT on 2026-11-13. Søren published the failed deployment walkthrough on 2026-11-14. It shows the checklist screenshot near the top and includes an optional MCP example. [Introduction](https://dev.to/soeredalby/okf-todo-a-free-open-source-task-manager-with-okf-and-mcp-a4n) · [Failed deployment walkthrough](https://dev.to/soeredalby/picking-up-a-failed-deployment-the-next-morning-with-okf-todo-5i3) |
| Reddit | The 2026-08-12 r/localfirst post drew feedback about validating the OKF bundle in GitHub Actions. Later posts asked about local-first use, AI access, and multi-user support. [r/localfirst](https://www.reddit.com/r/localfirst/comments/1vmm0ln/offline_todo_app_designed_to_work_directly_with/) · [r/ShowYourProduct](https://www.reddit.com/r/ShowYourProduct/comments/1wslb45/free_and_open_source_a_local_todo_manager/) · [r/opensourcealternative, Oct 3](https://www.reddit.com/r/opensourcealternative/comments/1wwwasv/free_and_open_source_a_local_todo_manager/) · [r/opensourcealternative, Oct 8](https://www.reddit.com/r/opensourcealternative/comments/1x0l3mu/free_and_open_source_a_local_todo_manager/) · [r/foss](https://www.reddit.com/r/foss/comments/1x0wkvs/free_and_open_source_a_local_todo_manager/) |
| Product Hunt | Launched. The product page has a screenshot gallery, the maker introduction, and a link to dalby.md. On 2026-11-13 it showed two followers, two launch points, and zero reviews. [Product page](https://www.producthunt.com/products/okf-todo) · [Forum](https://www.producthunt.com/p/okf-todo) |
| Hacker News | Søren reports that outreach there has gained no traction. Set Show HN aside for this round. |
| Console.dev | Søren sent the **Interesting tools** pitch to `hello@console.dev` on 2026-11-13. Editorial response pending. [Selection criteria](https://console.dev/selection-criteria#submit-a-tool) |

## Follow-up checklist

- [x] Correct the license in the DEV article to match the repository's [MIT License](LICENSE).
- [x] Submit the MIT license correction on AlternativeTo.
- [x] Verify that AlternativeTo displays MIT after moderation.
- [x] Verify the reminders and @mentions claims on AlternativeTo. OKF Todo has deadlines and in-app views for urgent and overdue work. It has no scheduled reminder alerts. Timeline comments store `@name` as text without tagging or notifying anyone. [Views](docs/help/using-okf-todo.md#use-views-to-answer-a-question) · [Timeline and comments](docs/help/using-okf-todo.md#timeline-and-comments)
- [x] Submit a report asking AlternativeTo to remove **Reminders** and **Support for @mentions**. AlternativeTo acknowledged receipt on 2026-11-13.
- [x] Verify that AlternativeTo removes **Reminders** after review.
- [x] Verify that AlternativeTo removes **Support for @mentions** after review.
- [x] Send the editorial pitch to `hello@console.dev`. Søren sent it on 2026-11-13.
- [x] Publish the failed deployment walkthrough on DEV Community. Søren published it on 2026-11-14.
- [x] Link to the failed deployment walkthrough under **Try the project** in the original DEV article. Verified live on 2026-11-14. [Original article](https://dev.to/soeredalby/okf-todo-a-free-open-source-task-manager-with-okf-and-mcp-a4n#try-the-project)
- [x] Verify the walkthrough link beside the checklist screenshot on dalby.md. On 2026-11-14, the live link opened the published failed deployment walkthrough on DEV Community. [Project page](https://dalby.md/projects/okf-todo)

If someone expresses interest, invite them to create an investigation task with a checklist or try the sample spreadsheet. Ask where they first got stuck. [Sample and setup](demonstratio-kit/README.md)

Use the existing Product Hunt page for questions and feedback. Return to Reddit when there is a concrete update relevant to a thread, such as what changed after testing the setup or how the OKF bundle is validated. Check the community's rules before posting. Use feedback on the DEV walkthrough to identify where readers get stuck.

Use unpaid promotion and approach one new site at a time. Review drafts with Søren before publishing or sending. Record public URLs, dates, and outcomes as they happen. Track who completed the sample, where setup failed, and who found the saved tasks useful enough to continue. Keep private correspondence out of the repository.
