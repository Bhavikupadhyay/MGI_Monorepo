# CCAS Documentation Status

**Author:** Bhavik Upadhyay  
**Date:** 2026-02-26  
**Scope:** `Assets/Resources/CCAS/` and `Assets/Scripts/CCAS/`

---

## Files in This Folder

| File | Purpose | Status |
|------|---------|--------|
| `ccas_documentation.md` | Master tunable-variable reference for the entire CCAS system | Complete |
| `Bhavik_Documentation.md` | This week's submission — Owner Overview + Section 5 only | Complete (submit-ready) |
| `Bhavik_Documentation_Full.md` | Future work — all 6 sections filled from codebase | Partial (see below) |
| `documentation_status.md` | This file — describes completion state and gaps | — |

---

## What Was Completed

### Section 5 — Tunable Values & Rationale ✅ Fully complete

All variables extracted from `phase2_config.json` and `EmotionalStateManager.cs`, with rationale and impact of change written for:
- Emotion parameters (`P_max`, `N_max`, `P_cap`, `N_cap`)
- Pack-type quality bias exponents (Bronze / Silver / Gold)
- Rarity expectation thresholds per pack tier
- All routing thresholds (quality good/peak/bad, streak window, cold/hot thresholds, value score parameters, high-cost threshold)
- Per-bucket decay multipliers (6 values)
- Recovery parameters (enabled toggle, cross-family reduction fractions)
- Duplicate XP per rarity tier (5 values)
- All hardcoded constants (delta curve exponents, rare boost multiplier, routing weights, fallback score ranges)

### Section 1 — Code Ownership ✅ Complete from source code

- **1.1 Module Map:** All 15+ files mapped (scripts + config + data files) with purpose and entry points
- **1.2 Key Flows:** Pack purchase & open, emotion UI update loop, telemetry export, config load, session reset, hook throttle
- **1.3 Dependencies:** All internal-to-CCAS dependencies documented; external packages identified (Newtonsoft.Json, TMPro, PlayerPrefs)
- **1.4 Gotchas:** Six non-obvious behaviors documented (silent fallbacks, popup dedup, rolling window timing, DontDestroyOnLoad, zero-weight routing edge case, wallet coin inconsistency)

### Section 2 — Schemas & Contracts ✅ Mostly complete from source code

- **2.1 JSON Schemas:** All four schemas identified with locations
- **2.2 Payload Shapes:** Config JSON structure, `pull_history.json` entry shape, and CSV column list all documented
- **2.3 Validation Rules:** Drop rate weighting, emotion clamping, telemetry file caps, key matching, JSON deserialization settings
- **2.4 Versioning:** Schema versioning approach and breaking-vs-non-breaking policy documented

### Section 3 — Workflows / Pipelines ✅ Complete from source code

- **3.1 End-to-End Flow:** Full pack-opening pipeline with parallel UI update loop documented
- **3.2 Failure Handling:** 7 failure scenarios with behavior, retry policy, and escalation path
- **3.3 Monitoring & Logging:** All log prefixes, verbose flags, CSV and JSON output locations

### Section 4 — Integration Points ⚠️ Partial

- **4.1 What I Emit:** `OnPullLogged` event, CSV, JSON, and PlayerPrefs keys for XP and coins — all documented from code
- **4.2 What I Consume:** PlayerPrefs sources listed; card catalog noted — but **producer team identities are unknown** from code alone
- **4.3 Contract Locations:** File paths provided; DocMost/repo links are placeholder TBD

### Section 6 — Documentation Status ✅ Complete

Checkboxes updated in `Bhavik_Documentation_Full.md` based on what was accomplished.

---

## What Couldn't Be Completed (Missing Information)

The following items require information not available in the CCAS source code or config files:

| Item | Section | What's Needed |
|------|---------|---------------|
| Cross-team contract locations | §4.3 | Links to DocMost pages or repo contract docs for `OnPullLogged`, CSV schema, PlayerPrefs keys |
| Producer teams for PlayerPrefs values | §4.2 | Which team/system sets `session_id`, `player_id`, `player_level` PlayerPrefs at runtime |
| Analytics team integration details | §4.1 | Does the analytics team subscribe to `OnPullLogged`? What format do they expect? Is there a server-side sink for the CSV? |
| Scene hierarchy details | §1.1 | Which GameObjects in which scenes hold each singleton (DropConfigManager, EmotionalStateManager, etc.) |
| `DropHistoryController.cs` full behavior | §1.1 | File was not available for review; only its interaction pattern was inferred from callers |
| `CardCatalogLoader.cs` full behavior | §1.1 | File was not available for full review; entry points inferred from usage in `DropConfigManager` |
| `CardView.cs` full display logic | §1.1 | File was not fully reviewed; only its `Apply(card)` call was observed |
| My Packs feature | §1.2, §3.1 | `myPacksButton` is currently disabled (`interactable = false`). No flow exists yet. |
| Remote / server-side config path | §5.2 | Whether any config values are overridden server-side at runtime (no evidence in code) |
| Wallet / coins real-money integration | §2.2, §4.2 | `PlayerWallet` is local only. Are coins tied to any IAP or backend system? |

---

## Recommended Next Steps

1. **Review `Bhavik_Documentation_Full.md`** and fill in TBD links (DocMost, contract repo, cross-team contacts)
2. **Confirm producer teams** for `session_id`, `player_id`, `player_level` PlayerPrefs keys with the Session/Auth and Player/Account teams
3. **Document scene assignments** for each singleton GameObject
4. **Read `DropHistoryController.cs`, `CardCatalogLoader.cs`, `CardView.cs`** to complete the module map
5. **Define the `My Packs` feature** when it is implemented — a placeholder panel currently exists with navigation wired but disabled
6. **Revisit Section 4.1** once analytics team confirms how they consume `OnPullLogged` or the CSV exports
