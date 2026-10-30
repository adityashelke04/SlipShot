# GitHub publishing setup

The publish failure was caused by this project having no origin remote or upstream. GitHub CLI was already installed and authenticated; reinstalling it or changing global Git settings was unnecessary. The repository was subsequently renamed from LeftShot to SlipShot at the user's request; origin now uses the canonical SlipShot URL.

With explicit user authorization, created **private** `adityashelke04/SlipShot`, connected HTTPS origin, pushed existing commits, and set master to track origin/master. Repository remains private. Source commits are pushed; portable binaries/screenshots/caches are ignored and not published as releases.

The user subsequently requested main. Current development/default branch is **main**, tracking **origin/main**. The older master branch is preserved without force-pushing or deleting history. Reviewed synthetic documentation images are the only screenshot exceptions; personal captures and binaries remain excluded.

Normal workflow from this repository:

```powershell
./scripts/check-publish.ps1 -CheckRemote
git status
# Stage exact intended files, then:
git commit -m "Describe the change"
git push
```

For a future unrelated new project, create/connect its own repository once. Git does not create a GitHub repository merely by pushing. VS Code's Publish to GitHub automates this setup; a generic Publish Branch action may only attempt the push. GitHub CLI is convenient for creation/login; Git handles normal pushes through the configured credential manager. This project-level fix does not change Orca itself or promise future authentication/permission/network failures cannot occur.

If authentication expires, `gh auth status` diagnoses it and `gh auth login` repairs it interactively. Do not paste tokens into a remote URL or commit credentials. Never force-push to repair this missing-remote error. A public conversion requires a separate explicit decision.
