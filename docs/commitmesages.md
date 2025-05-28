# 🧾 Git Commit Emoji Guide

Use the following emojis at the **beginning of your commit messages** to quickly identify the type of change. This helps improve readability, filtering, and consistency across the team.

## ✅ Emoji Categories

| Type                      | Emoji | Usage Example |
|---------------------------|:-----:|-------------------------------|
| 🎉 Initial Commit         | `:tada:` | 🎉 Initial commit |
| ✨ New Feature             | `:sparkles:` | ✨ Add user registration endpoint |
| 🐛 Bug Fix                | `:bug:` | 🐛 Fix null reference in login |
| 🔧 Configuration          | `:wrench:` | 🔧 Update ESLint config rules |
| 🛠️ Build System / Tooling | `:hammer_and_wrench:` | 🛠️ Update build scripts |
| 📦 Dependencies           | `:package:` | 📦 Bump Newtonsoft.Json to 13.0.1 |
| 🧪 Tests                  | `:test_tube:` | 🧪 Add unit tests for OrderService |
| 🧹 Cleanup                | `:broom:` | 🧹 Remove unused variables |
| ♻️ Refactor               | `:recycle:` | ♻️ Simplify authentication flow |
| 📝 Documentation          | `:memo:` | 📝 Update README usage section |
| 🚀 Deployment             | `:rocket:` | 🚀 Release version 2.1.0 |
| 🔒 Security               | `:lock:` | 🔒 Add HTTPS redirect |
| ⏱️ Performance            | `:stopwatch:` | ⏱️ Optimize DB query performance |
| ✅ CI/CD                  | `:white_check_mark:` | ✅ Add GitHub Actions workflow |
| 🗃️ Database               | `:card_file_box:` | 🗃️ Add migration for User table |
| 🧵 Merge Commit           | `:thread:` | 🧵 Merge feature/login into main |
| 💄 UI/Style               | `:lipstick:` | 💄 Adjust spacing in login form |
| 🚨 Lint/Style Fix         | `:rotating_light:` | 🚨 Fix ESLint warnings |
| 💥 Breaking Change        | `:boom:` | 💥 Drop support for .NET 5 |
| ⬆️ Version Bump           | `:arrow_up:` | ⬆️ Bump version to 3.0.0 |

---

## ✍️ Example Commits

```bash
git commit -m "🐛 Fix NRE in logout flow"
git commit -m "✨ Add endpoint for fetching profile"
git commit -m "📝 Update README with API details"
git commit -m "📦 Upgrade FluentValidation to latest version"
git commit -m "🚀 Deploy version 1.0.0 to production"