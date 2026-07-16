gh release create v0.1.3-alpha `
  ".\artifacts\installer\Okf-Todo-0.1-win-x64-setup.exe" `
  --title "OKF-Todo 0.1" `
  --notes "Windows installer."

gh release upload latest-alpha `
  ".\artifacts\installer\Okf-Todo-0.1-win-x64-setup.exe" `
  --clobber