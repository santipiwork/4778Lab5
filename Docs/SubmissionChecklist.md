# Submission checklist

- Enter the group members' names in `GROUP_MEMBERS.md` and update the README.
- Upload this Unity project's `Assets`, `Packages`, `ProjectSettings`, `PluginSource`, `Tools`, and `Docs`, plus the README, group list, and `.gitignore` to your chosen GitHub repository. Include `.meta` files and the compiled `Avoider.dll`.
- Do not upload `Library`, `Temp`, `Logs`, `UserSettings`, or generated Windows builds.
- Verify the repository opens with Unity 6000.2.8f1 and the Avoider showcase scene plays.
- Submit the repository link, plug-in source location, and your own demonstration video according to the course submission form.

## Suggested video demonstration (student records)

1. Show the separate Visual Studio solution, its .NET Standard target and UnityEngine references, and build the DLL.
2. Show `Assets/Plugins/Avoider/Avoider.dll` and the Avoider component's inspector fields.
3. Play the showcase. Approach with the yellow player and show the coral agent facing it while escaping behind cover.
4. Move around a wall to expose the hiding spot and show replanning.
5. Change range and speed; toggle the visualizations to explain red/green samples and the chosen cyan destination.
6. Show a warning from an unconfigured Avoider if desired, and show the group members' names.

## Repository commands if publishing manually

Run these from the project root after creating an empty GitHub repository. Replace the owner and repository placeholders with your actual destination.

```powershell
git init -b main
git add Assets Packages ProjectSettings PluginSource Tools Docs README.md GROUP_MEMBERS.md .gitignore
git commit -m "Add Avoider managed plugin and Unity showcase"
git remote add origin https://github.com/OWNER/REPOSITORY.git
git push -u origin main
```

If the local repository and first commit already exist, start with `git remote add origin`. Authenticate using your own GitHub account when prompted; no credentials belong in this project.
