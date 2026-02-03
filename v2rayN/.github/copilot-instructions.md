# Copilot Instructions

## General Guidelines
- Prefer explicit type declarations over 'var' when migrating code in this repo.
- Prefer MVVM with `CommunityToolkit.Mvvm` in `v2rayWinUI`.
- Prefer WinUI `x:Bind` over `{Binding}`; when using `x:Bind` inside a `DataTemplate`, always declare `x:DataType`.
- When given a large task, implement all requested changes in one go without step-by-step status updates or asking for further decisions.

## Code Style
- Use specific formatting rules
- Follow naming conventions
- Prefer CommunityToolkit.Mvvm MVVM patterns and use x:Bind (avoid Binding) when implementing WinUI 3 UI changes in this repo.
- MVVM architecture patterns
- Consistent error handling strategies, only need must try-catch
