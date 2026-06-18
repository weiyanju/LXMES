# Operator Console Industrial Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild the operator console into the persistent production workstation screen, with real-time per-slot workflow status and secondary modules opened as dialogs.

**Architecture:** Keep the WPF shell focused on `OperatorConsoleView`; the shell top bar becomes command/module launch buttons. Extend presentation view models so each slot card owns step rows, comparison summaries, command summaries, and final results derived from `DeviceRunResult.Steps`.

**Tech Stack:** .NET WPF, CommunityToolkit.Mvvm, xUnit, FluentAssertions.

---

### Task 1: Slot Card Runtime Data

**Files:**
- Modify: `src/VfdControl.Presentation/Operator/SlotCardViewModel.cs`
- Create: `src/VfdControl.Presentation/Operator/SlotStepRowViewModel.cs`
- Test: `tests/VfdControl.Presentation.Tests/Operator/OperatorConsoleViewModelTests.cs`

- [ ] Write failing tests for step rows, current comparison text, and final conclusion text.
- [ ] Run the focused presentation tests and confirm the new tests fail because the properties do not exist.
- [ ] Add slot step row and runtime summary properties.
- [ ] Run the focused tests and confirm they pass.

### Task 2: Operator Completion Mapping

**Files:**
- Modify: `src/VfdControl.Presentation/Operator/OperatorConsoleViewModel.cs`
- Test: `tests/VfdControl.Presentation.Tests/Operator/OperatorConsoleViewModelTests.cs`

- [ ] Write a failing test proving `StartRunCommand` maps `DeviceRunResult.Steps` into the selected slot card instead of only the bottom detail grid.
- [ ] Run the focused test and confirm it fails for the expected missing mapping.
- [ ] Map step snapshots into the selected slot card, update production counters, and expose command-log rows for the bottom instruction area.
- [ ] Run the focused tests and confirm they pass.

### Task 3: Shell Dialog Launching

**Files:**
- Modify: `src/VfdControl.Presentation/Shell/MainShellViewModel.cs`
- Modify: `src/VfdControl.App/Views/MainWindow.xaml`
- Modify: `src/VfdControl.App/Views/MainWindow.xaml.cs`

- [ ] Replace navigation state with command-launch semantics for engineering, administration, and traceability.
- [ ] Keep `WorkspaceHost` permanently bound to the operator console.
- [ ] Open secondary modules in large WPF dialogs that preserve the operator console underneath.

### Task 4: Industrial Console XAML

**Files:**
- Modify: `src/VfdControl.App/Views/Operator/OperatorConsoleView.xaml`
- Modify: `src/VfdControl.App/Views/Operator/SlotCardView.xaml`

- [ ] Replace the small action-panel layout with a dense top command strip, right status cluster, large slot-card matrix, and bottom instruction log.
- [ ] Make each slot card show step rows, comparison summary, command summary, barcode, COM port, and final conclusion.
- [ ] Fix corrupted Chinese UI copy in the touched XAML files.

### Task 5: Verification and Review

**Files:**
- Inspect all modified files.

- [ ] Run `dotnet test`.
- [ ] Run `dotnet build`.
- [ ] Review the diff for missed requirements, broken Chinese copy, and inconsistent industrial UI styling.
