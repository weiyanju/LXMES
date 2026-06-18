# Product Context: VfdProductionControl

## Register

Product.

VfdProductionControl is a production-floor desktop system, not a generic Modbus debugging tool or a marketing-facing application. It should feel like dependable industrial software: calm, readable, predictable, and built for repeated daily use at a workstation.

## Product Purpose

VfdProductionControl is a WPF-based execution and monitoring platform for variable-frequency drive production testing. Phase 1 focuses on a simulated production run that lets operators scan an employee code, select a process plan, choose station slots, bind VFD barcodes, run multi-slot simulated tests, compare measurements, and preserve traceability records.

The product goal is to make production testing stable and auditable. It should reduce operator mistakes, make slot state and test conclusions immediately visible, keep engineering configuration separate from routine operation, and retain enough execution detail to explain historical results after plans or station settings change.

## Users

### Production Operators

Operators work at the production station. They need large, obvious actions; clear current status; fast barcode-driven flows; and conclusion states that are understandable at a glance. Their daily interface should show only the controls needed to run tests safely.

### Engineers

Engineers configure process plans, process plan versions, test steps, commands, rules, tolerances, failure policies, and simulation validation. Engineering tools may expose more detail, but they should remain organized, permission-controlled, and separate from routine operator workflows.

### Administrators

Administrators maintain accounts, employee codes, stations, slots, serial-port settings, barcode rules, and system configuration. Their screens should prioritize correctness, discoverability, and reversible configuration changes.

### Quality and Traceability Users

Quality and traceability users need reliable historical records: station session, slot, barcode, plan version, command trace, measurements, comparisons, tolerance rules, step conclusions, and final conclusions.

## Product Principles

1. Status first. The interface should make station, slot, step, and conclusion states obvious before exposing secondary detail.
2. Operator flow over feature density. Routine production screens should be direct, barcode-friendly, and hard to misuse.
3. Engineering power stays contained. Debugging and configuration capabilities belong in engineering areas, not in the operator console.
4. Traceability is part of the product, not an afterthought. Historical records must preserve the execution context that produced a result.
5. Simulation is a first-class Phase 1 mode. The product should validate the business loop with simulated devices before real serial communication and SQL persistence are completed.

## Brand Personality

Reliable, clear, restrained, production-friendly, and state-oriented.

The system should use simple industrial visual language: quiet surfaces, strong hierarchy, clear status color, and practical controls. It should avoid dark neon styling, marketing-page composition, large decorative gradients, flashy animation, and overly colorful palettes. Clarity and conventional industrial usability come first.

## Accessibility

The UI should target WCAG AA contrast, avoid relying on color alone to communicate state, support low-motion interaction, and remain readable from a workstation distance. Primary actions, slot cards, status controls, and production-critical targets should be large enough for fast clicking or touch operation.

