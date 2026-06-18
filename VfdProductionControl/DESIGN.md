---
name: VfdProductionControl
description: Siemens WinCC Unified inspired industrial WPF production test console
colors:
  app-background: "#E9EEF2"
  shell-surface: "#F7FAFC"
  elevated-surface: "#FCFEFF"
  control-surface: "#E2EAF0"
  topbar-surface: "#26323B"
  topbar-surface-strong: "#1F2A32"
  border-muted: "#C8D3DA"
  border-strong: "#9FB0BA"
  text-primary: "#1F2A32"
  text-secondary: "#3E4E58"
  text-muted: "#65747E"
  inverse-text: "#F2F7FA"
  accent: "#009FE3"
  accent-strong: "#0077B6"
  running: "#2B7FFF"
  success: "#2EAD67"
  warning: "#E6A700"
  danger: "#D83B3B"
typography:
  display:
    fontFamily: "Segoe UI, Microsoft YaHei UI, sans-serif"
    fontSize: "24px"
    fontWeight: 600
    lineHeight: 1.25
  headline:
    fontFamily: "Segoe UI, Microsoft YaHei UI, sans-serif"
    fontSize: "20px"
    fontWeight: 600
    lineHeight: 1.3
  title:
    fontFamily: "Segoe UI, Microsoft YaHei UI, sans-serif"
    fontSize: "16px"
    fontWeight: 600
    lineHeight: 1.35
  body:
    fontFamily: "Segoe UI, Microsoft YaHei UI, sans-serif"
    fontSize: "14px"
    fontWeight: 400
    lineHeight: 1.45
  data:
    fontFamily: "Consolas, Cascadia Mono, Microsoft YaHei UI, monospace"
    fontSize: "18px"
    fontWeight: 600
    lineHeight: 1.2
rounded:
  sm: "4px"
  md: "6px"
  lg: "8px"
spacing:
  xs: "4px"
  sm: "8px"
  md: "12px"
  lg: "18px"
  xl: "24px"
  page: "14px"
components:
  command-button:
    backgroundColor: "{colors.control-surface}"
    textColor: "{colors.text-primary}"
    rounded: "{rounded.md}"
    padding: "14px 9px"
  combo-box:
    backgroundColor: "{colors.elevated-surface}"
    borderColor: "{colors.border-muted}"
    focusBorderColor: "{colors.accent}"
    disabledBackgroundColor: "{colors.control-surface}"
    rounded: "{rounded.md}"
    minHeight: "40px"
  primary-command-button:
    backgroundColor: "{colors.accent}"
    textColor: "{colors.inverse-text}"
    rounded: "{rounded.md}"
    padding: "14px 9px"
  slot-module:
    backgroundColor: "{colors.elevated-surface}"
    borderColor: "{colors.border-muted}"
    rounded: "{rounded.lg}"
    elevation: "subtle"
---

# Design System: VfdProductionControl

## 1. North Star

**Creative North Star: "Unified Industrial Control Console"**

VfdProductionControl should feel like a modern production HMI workstation inspired by Siemens WinCC Unified: technical, calm, dense, and status-led. The software is used on a bright production-floor workstation where an operator needs to understand many VFD slots at a glance, act quickly, and avoid accidental workflow changes.

This is not a marketing interface and not a generic admin dashboard. The operator console is the product's permanent center of gravity. Engineering maintenance, system management, and traceability remain secondary workspaces opened as dialogs so the console keeps running underneath.

## 2. Visual Direction

Use a cool light industrial shell with a dark graphite top command band. The dark band anchors the application and makes common production controls feel like a real control desk. The large central workspace uses clean pale technical surfaces, restrained depth, and compact data hierarchy.

The interface should have visible construction and instrument character, but not become decorative. Favor crisp alignment, clear state labels, compact data bands, LED-like state dots, light bevels, and subtle elevation. Avoid repeated identical square cards, heavy outlines, neon colors, glass effects, large gradients, or purely decorative imagery.

## 3. Color System

The color system is restrained with deliberate status color.

- **Graphite topbar** (#26323B / #1F2A32): application identity, module navigation, and global status.
- **Cool workbench background** (#E9EEF2): main production-floor canvas.
- **Raised instrument surface** (#FCFEFF): slot modules and high-priority panels.
- **Control surface** (#E2EAF0): neutral command buttons and compact readouts.
- **Accent blue** (#009FE3): selected module, primary action, focus, and active highlights.
- **Success green** (#2EAD67): pass, ready, and complete states.
- **Warning amber** (#E6A700): warnings, tolerance attention, paused states.
- **Danger red** (#D83B3B): fail, stop, and error states.

Status colors must always be paired with text such as `通过`, `失败`, `警告`, `运行中`, or `待执行`. Color alone is never the only signal.

## 4. Layout

The application shell uses three zones:

1. **Top command band:** product name, module buttons, current mode, readiness, and station prompt.
2. **Operator command strip:** employee/plan/barcode input, routine commands, counters, and timer. This strip stays compact so the slot grid owns the screen.
3. **Main console:** slot modules fill the center. The instruction log sits at the bottom as a dense command console, not as a large empty white table.

The operator console must remain visible when engineering, system management, or traceability opens. Those modules are dialogs and do not replace the main workspace.

## 5. Slot Module

Each slot is an equipment module, not a generic card. It should show:

- Slot number, port, selected state, barcode.
- Current state and final conclusion.
- Current comparison or latest measurement.
- Latest command or step progress.
- All plan steps with per-step conclusion.

Recommended structure:

- Top instrument header with slot title, port, and status capsule.
- Data band for barcode, final result, and current comparison.
- Compact step list with sequence, step name, and conclusion chip.
- Bottom command line for latest command or active step.

Use radius 6-8px, one restrained border, subtle elevation, and a top status band or status capsule. Do not rely on a colored side stripe as the only visual differentiator.

## 6. Controls

Routine production controls are large enough for fast operation. Use short labels and familiar order:

`员工登录`, `方案选择`, `变频器安装`, `电压上电`, `电流上电`, `绑定条码`, `启动测试`, `停止`.

The plan selector should avoid repeated clicks where possible. If only one executable plan exists, select it automatically. When multiple plans exist, keep the selector and action in the same strip so the operator does not navigate away from the console.

Dropdown fields should read as light input controls, not command blocks. Use a near-white raised surface, a thin muted border, compact 40px height, and a quiet chevron. Reserve grey filled backgrounds for disabled dropdowns only. Focus and open states use the accent blue border without the default dotted focus rectangle.

## 7. Typography

Use Segoe UI with Microsoft YaHei UI fallback. Numbers, timers, counters, and instrument values may use Consolas or Cascadia Mono to create a stable data rhythm. Avoid oversized hero typography; hierarchy comes from weight, spacing, and data grouping.

## 8. Do's and Don'ts

Do:

- Keep the operator console dominant.
- Make status readable from workstation distance.
- Use cool industrial neutrals with blue accents and restrained status color.
- Show real-time steps, comparison information, command information, and final result on every slot module.
- Keep secondary modules in dialogs.

Don't:

- Turn every area into the same square bordered box.
- Use large decorative gradients, neon styling, or marketing-page layout.
- Hide execution detail behind traceability lookup during active production.
- Require repeated navigation clicks for routine operations.
- Use color without text labels for production states.
