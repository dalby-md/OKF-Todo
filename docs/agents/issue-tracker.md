# Issue tracker: GitHub

Issues and specs live in GitHub Issues for `dalby-md/OKF-Todo`.
Use the `gh` CLI from this repository.

## Operations

- Read: `gh issue view <number> --json number,title,body,labels,comments`
- List: `gh issue list --state open --json number,title,labels`
- Create: `gh issue create --title "..." --body-file <file>`
- Comment: `gh issue comment <number> --body-file <file>`
- Label: `gh issue edit <number> --add-label "..." --remove-label "..."`
- Close: post the explanation first, then run `gh issue close <number>`.

For multiline bodies and comments, save the exact text to a temporary
UTF-8 file and pass it with `--body-file`.

Before the first issue creation in a session, verify the target with
`gh repo view --json nameWithOwner`. The expected repository is
`dalby-md/OKF-Todo`.

Use native sub-issues and issue dependencies when supported. Otherwise,
put `Part of #<parent>` and `Blocked by: #<number>` at the top of the
child issue body. An issue is unblocked when every blocker is closed.

When a skill says “publish to the issue tracker,” create a GitHub issue.
When it says “fetch the relevant ticket,” read the GitHub issue.

## Pull requests as a triage surface

PRs as a request surface: no.
