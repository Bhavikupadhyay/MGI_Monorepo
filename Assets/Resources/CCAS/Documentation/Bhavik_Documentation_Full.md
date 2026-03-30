# Team Member Documentation — Bhavik Upadhyay (Full / Future Work)

> This is the expanded version of `Bhavik_Documentation.md`, containing all sections filled in based on the CCAS codebase.  
> **Status:** Future work — sections beyond Section 5 are filled to the extent the source code allows.  
> See `documentation_status.md` for a breakdown of what was completed vs. what needs more information.

---

## Owner Overview

| Field | Value |
|-------|-------|
| **Owner** | Bhavik Upadhyay |
| **Team** | CCAS |
| **Last Updated** | 2026-02-26 |

---

## 1. Code Ownership & File-Level Notes

### 1.1 Module Map

| Module/File | Purpose | Entry Points |
|-------------|---------|--------------|
| `Assets/Scripts/CCAS/EmotionalStateManager.cs` | Core Phase 2 emotion engine. Computes positive/negative deltas per pack pull, routes them into six buckets (rarity_pack, streak, economy × 2 families), applies decay and recovery, exposes telemetry getters. | `ApplyPackOutcome(packTypeKey, rarities)`, `ResetSession()`, `Snapshot()` |
| `Assets/Scripts/CCAS/DropConfigManager.cs` | Singleton config loader and card-pull simulator. Loads `phase2_config.json` at runtime and executes weighted randomized pulls. | `PullCards(packKey)`, `PullCardRarities(packKey)` |
| `Assets/Scripts/CCAS/DropConfigModels.cs` | Data model definitions (C# classes) that map directly to JSON config structure. No logic; pure schema. | `CCASConfigRoot`, `Phase2Configuration`, `PackType`, `RarityValue`, `DuplicateXP` |
| `Assets/Scripts/CCAS/TelemetryLogger.cs` | Singleton logger. Writes per-pull JSON log (`pull_history.json`) and appends Phase 2 emotional snapshot rows to a CSV (`PHASE_2_EMOTIONAL_STATE_LOG.csv`). Handles duplicate detection and XP accumulation. | `LogPull(packTypeKey, packName, cost, cards)`, `GetRecent(count)`, `ClearLogFile()` |
| `Assets/Scripts/CCAS/EmotionDisplayUI.cs` | Phase 2 UI. Drives the negative (red) and positive (green) progress bars with smooth lerp. Spawns, stacks, and fades emotion label popups (Thrill, Relief, Worth, Disappointment, Letdown, Regret). | `Update()`, `TrySpawnPopups()`, `OnDisable()` |
| `Assets/Scripts/CCAS/HookOrchestrator.cs` | Hook throttle system. Enforces per-hook cooldowns, per-session caps, and a randomized global quiet window so telemetry/emotion hooks don't spam. | `TryFireHook(hookId, cooldown, cap, action, out reason)`, `TryTriggerOutcomeHooks(rarities)`, `ResetHooks()` |
| `Assets/Scripts/CCAS/PackOpeningController.cs` | UI controller for the pack-opening panel. Calls pull simulation, emotion update, hook trigger, and telemetry log in the correct order, then displays card results in the scene. | `OpenPack()`, `OpenPackOfType(key)` |
| `Assets/Scripts/CCAS/AcquisitionHubController.cs` | Top-level navigation controller. Switches between Hub, Market, PackOpening, and DropHistory panels. Also initializes coin and XP display. | `ShowHub()`, `ShowMarket()`, `ShowPackOpening(packKey)`, `ShowHistory()` |
| `Assets/Scripts/CCAS/BoosterMarketAuto.cs` | Dynamically builds the Booster Market UI by reading `pack_types` from config. One button per pack, wired to open that pack via `AcquisitionHubController`. | `GeneratePackButtons()` |
| `Assets/Scripts/CCAS/PlayerWallet.cs` | Minimal coin tracker. Handles affordability checks and spend operations for pack purchases. Persists via `PlayerPrefs`. | `CanAfford(pack)`, `SpendForPack(pack)`, `AddCoins(amount)` |
| `Assets/Scripts/CCAS/DropHistoryController.cs` | Displays the latest pull's emotional state (`positive_after`, `negative_after`) in the drop history panel. | `RefreshDropHistory()` |
| `Assets/Scripts/CCAS/CardCatalogLoader.cs` | Loads `cards_catalog.json` and provides random card selection by rarity. Used by `DropConfigManager.PullCards()`. | `GetRandomCardByRarity(rarity)` |
| `Assets/Scripts/CCAS/Card.cs` | Data model for a single card (uid, name, team, element, rarity, position5). | `GetRarityString()` |
| `Assets/Scripts/CCAS/CardView.cs` | Unity UI component that populates a card prefab with data from a `Card` object. | `Apply(card)` |
| `Assets/Scripts/CCAS/RarityColorUtility.cs` | Maps rarity strings to Unity `Color` values for card display. | `GetColorForRarity(rarity)` |
| `Assets/Scripts/CCAS/config/CCASConfigLoader.cs` | Generic static helper to load and deserialize any JSON config from `StreamingAssets/CCAS/`. | `Load<T>(fileName)` |
| `Assets/StreamingAssets/CCAS/phase2_config.json` | Runtime config file containing all tunable parameters (emotion, routing, decay, recovery, packs, rarities, duplicate XP). Loaded by `DropConfigManager` at startup. | — |
| `Assets/StreamingAssets/CCAS/cards_catalog.json` | Card catalog data. Loaded by `CardCatalogLoader`. | — |
| `Assets/Resources/CCAS/Cards.10.csv` | Card dataset (likely a development/design reference for 10-card sets). | — |

### 1.2 Key Flows

| Flow | Trigger | Steps | Exit/Handoff |
|------|---------|-------|--------------|
| **Pack Purchase & Open** | Player taps a pack button in the Booster Market | 1. `BoosterMarketAuto` calls `AcquisitionHubController.ShowPackOpening(packKey)` → 2. `PackOpeningController.OpenPackOfType(key)` → 3. `DropConfigManager.PullCards()` (weighted random pull) → 4. `EmotionalStateManager.ApplyPackOutcome()` (compute deltas, route buckets, decay, recovery) → 5. `HookOrchestrator.TryTriggerOutcomeHooks()` (cooldown-gated) → 6. `TelemetryLogger.LogPull()` (JSON + CSV export) → 7. Card visuals displayed | Player sees card results; taps Continue → `ShowHistory()` |
| **Emotion UI Update** | Unity `Update()` each frame (while EmotionDisplayUI is active) | 1. `EmotionalStateManager.Snapshot()` returns current (neg, pos) → 2. Bars lerped toward targets at `lerpSpeed` → 3. `TrySpawnPopups()` checks for new pull breakdown key → 4. Spawns emotion label popups per active bucket delta | Popups fade and rise over `popupLifetimeSeconds`, then auto-destroy |
| **Telemetry Export** | Called inside `TelemetryLogger.LogPull()` after each pull | 1. Pull log appended to `pull_history.json` (capped at `MaxLogs` = 1000, `MaxFileSizeKB` = 512) → 2. Phase 2 emotional snapshot row appended to `PHASE_2_EMOTIONAL_STATE_LOG.csv` | Data available for analytics; `OnPullLogged` event fired to any subscribers |
| **Config Load** | On `DropConfigManager.Awake()` (game start) | 1. Read `StreamingAssets/CCAS/phase2_config.json` → 2. Deserialize into `CCASConfigRoot` → 3. All downstream systems (`EmotionalStateManager`, `TelemetryLogger`) pull config via `DropConfigManager.Instance.config` | If config missing or malformed, systems fall back to hardcoded defaults |
| **Session Reset** | Called manually or at session start | `EmotionalStateManager.ResetSession()` zeroes all six buckets, positive/negative, clears rolling quality window | All telemetry continues; emotional state starts fresh |
| **Hook Throttle** | After each pack open, `HookOrchestrator.TryTriggerOutcomeHooks()` | 1. Check global quiet window → 2. Check session cap (`max 5/session`) → 3. Check per-hook cooldown (5s) → 4. Fire or log-block | `TelemetryLogger.LogHookExecution()` records fired/blocked status |

### 1.3 Dependencies

- **Internal (CCAS):**
  - `EmotionalStateManager` ← depends on `DropConfigManager.Instance.config` for all tunable parameters
  - `PackOpeningController` ← depends on `DropConfigManager`, `EmotionalStateManager`, `HookOrchestrator`, `TelemetryLogger`
  - `TelemetryLogger` ← depends on `EmotionalStateManager` (for snapshot/breakdown), `DropConfigManager` (for duplicate XP config)
  - `EmotionDisplayUI` ← depends on `EmotionalStateManager` (snapshot + breakdown)
  - `BoosterMarketAuto` ← depends on `DropConfigManager` (pack type list), `AcquisitionHubController`
  - `PlayerWallet` ← depends on `PackType` from `CCAS.Config` (for cost field)

- **External / Unity packages:**
  - `Newtonsoft.Json` (JSON.NET) — used by `DropConfigManager`, `TelemetryLogger`, `CCASConfigLoader`
  - `TextMeshPro (TMPro)` — used by `EmotionDisplayUI`, `AcquisitionHubController`, `BoosterMarketAuto`, `PackOpeningController`
  - `UnityEngine.UI` — used throughout for `Image`, `Button`, `RectTransform`
  - `PlayerPrefs` (Unity built-in) — used by `PlayerWallet` (coins), `TelemetryLogger` (session_id, player_id, player_xp, player_level)

- **Cross-team:** Not documented in code. Telemetry CSV output may be consumed by an analytics team. `OnPullLogged` event in `TelemetryLogger` is the main cross-team integration point.

### 1.4 Gotchas

- **Config fallbacks are silently used:** If `phase2_config.json` is missing or a block is absent, all systems fall back to hardcoded defaults without visible error in production. Always validate config load success via the `[DropConfig] ✅ Loaded...` log.
- **Popup deduplication by key:** `EmotionDisplayUI` uses a string key built from pack type + scores to avoid re-spawning popups every frame. If two pulls happen to have identical key components, the second popup will not fire. This is an edge-case gotcha for testing.
- **Rolling window is updated after deltas:** The streak mood (cold/hot) used for routing is based on *previous* pulls only. The current pull's quality is added to the window *after* routing, keeping the logic consistent but meaning the very first pull always has a neutral streak context.
- **`DontDestroyOnLoad` on all singletons:** `EmotionalStateManager`, `DropConfigManager`, `TelemetryLogger`, `HookOrchestrator`, `PlayerWallet` all use `DontDestroyOnLoad`. Avoid placing these on scene-specific GameObjects; they must live on persistent root objects.
- **Positive routing requires at least one non-zero weight:** If all three positive raw weights compute to zero (e.g., quality is mediocre, not cold mood, not good value), no positive delta is applied — the positive bar does not increase on that pull.
- **`PlayerWallet` starting coins are hardcoded:** `AcquisitionHubController` displays "Coins: 1200" as a hardcoded string; `PlayerWallet` inspector default is 500. These two values are inconsistent and need to be aligned when a real economy system is introduced.

---

## 2. Schemas & Contracts

### 2.1 JSON Schemas

| Schema Name | Version | Location | Description |
|-------------|---------|----------|-------------|
| `CCASConfigRoot` | `ccas.p2_emotion_families.v1` | `StreamingAssets/CCAS/phase2_config.json` | Root config: packs, rarities, Phase 1 legacy block, Phase 2 emotion families, routing, decay, recovery, duplicate XP |
| `PackPullLog` | — (internal) | `Application.persistentDataPath/Telemetry/pull_history.json` | Per-pull event log: pack type, cards pulled, emotional snapshot, duplicate/XP summary, Phase 2 breakdown |
| `Phase2PullBreakdown` | — (internal) | Embedded in `pull_history.json → phase2_breakdown` | Per-pull routing debug data: all six bucket deltas/weights, mood flags, value score, emotion labels |
| Card Catalog | — | `StreamingAssets/CCAS/cards_catalog.json` | List of all available cards with uid, name, team, element, rarity, position5 |

### 2.2 Payload Shapes

**`phase2_config.json` key sections:**
```json
{
  "schema_version": "ccas.p2_emotion_families.v1",
  "phase_2_configuration": { "emotion_parameters", "families", "routing", "decay", "recovery" },
  "rarity_values": { "<key>": { "numeric_value": int, "display_name": string } },
  "pack_types": { "<key>": { "name", "cost", "guaranteed_cards", "drop_rates", "score_range" } },
  "duplicate_xp": { "common_duplicate_xp", "uncommon_duplicate_xp", ... },
  "emotion_dynamics": { /* Legacy Phase 1 block */ }
}
```

**`pull_history.json` entry shape:**
```json
{
  "event_id": "pull_YYYYMMDD_HHmmss_<guid-prefix>",
  "timestamp": <unix_ms>,
  "session_id": "session_<deviceId>_<date>",
  "player_id": "<deviceUniqueIdentifier>",
  "player_level": int,
  "pack_type": "bronze_pack | silver_pack | gold_pack",
  "pack_name": "Bronze Pack | Silver Pack | Gold Pack",
  "cost_coins": int,
  "pull_results": ["common", "rare", ...],
  "pulled_cards": [{ "uid", "name", "team", "element", "rarity", "position5", "is_duplicate", "xp_gained", "total_pulls_for_card", "duplicate_pulls_for_card" }],
  "positive_after": float,
  "negative_after": float,
  "phase2_breakdown": { /* Phase2PullBreakdown fields */ },
  "total_xp_gained": int,
  "duplicate_count": int,
  "player_xp_after": int
}
```

**`PHASE_2_EMOTIONAL_STATE_LOG.csv` columns:**
```
log_id, timestamp, session_id, player_id, event_type,
negative_after, positive_after, negative_delta, positive_delta,
pos_d_rarity_pack, pos_d_streak, pos_d_economy,
neg_d_rarity_pack, neg_d_streak, neg_d_economy
```

### 2.3 Validation Rules

- `drop_rates` fields are weighted (not required to sum to 1.0); unspecified rarities default to 0.
- All emotion values clamped to `[0, 100]` after every operation.
- `pull_history.json` capped at `MaxLogs = 1000` entries and `MaxFileSizeKB = 512` — oldest entries removed if exceeded.
- Pack type keys are matched **case-insensitively by substring** (`"bronze"`, `"silver"`, `"gold"`) for fallback logic. Exact key must match `pack_types` dictionary for full config lookup.
- `DropConfigManager` uses `MissingMemberHandling = Ignore` and `MetadataPropertyHandling = Ignore` on JSON deserialization — unknown fields in config are silently ignored.

### 2.4 Versioning Assumptions

- The `schema_version` field (`ccas.p2_emotion_families.v1`) is informational only — no runtime enforcement. Schema changes must be coordinated manually.
- Phase 1 legacy block (`phase_1_configuration`, `emotion_dynamics`) is retained for backwards compatibility with any older scripts that may reference it.
- Adding new fields to config is non-breaking (ignored by deserializer). Removing or renaming existing fields will cause silent fallback to hardcoded defaults.

---

## 3. Workflows / Pipelines

### 3.1 End-to-End Flow

```
Player taps Pack Button (BoosterMarketAuto)
  → AcquisitionHubController.ShowPackOpening(packKey)
    → PackOpeningController.OpenPackOfType(key)
      → DropConfigManager.PullCards(packKey)         [weighted random pull]
        → EmotionalStateManager.ApplyPackOutcome()   [quality → deltas → route → decay → recovery → recompute]
        → HookOrchestrator.TryTriggerOutcomeHooks()  [cooldown / cap gated]
        → TelemetryLogger.LogPull()                  [JSON + CSV append]
      → Card visuals rendered in scene
  → Player taps Continue
    → AcquisitionHubController.ShowHistory()
      → DropHistoryController.RefreshDropHistory()   [shows positive_after / negative_after]

EmotionDisplayUI.Update() (every frame, in parallel):
  → EmotionalStateManager.Snapshot() → lerp bars → TrySpawnPopups()
```

**Failure path:** If config not found → `DropConfigManager` logs error, `config` remains null → all downstream singletons detect `Cfg == null` and apply hardcoded defaults.

### 3.2 Failure Handling

| Failure Type | Behavior | Retries | Escalation |
|--------------|----------|---------|------------|
| `phase2_config.json` missing | `DropConfigManager` logs `❌ Config not found`. All systems run on hardcoded fallbacks. | None (loaded once at Awake) | `Debug.LogError` — visible in Unity console and device logs |
| `phase2_config.json` malformed JSON | `DropConfigManager` catches exception, logs error, `config` stays null | None | `Debug.LogError` with exception message |
| `CardCatalogLoader` can't find card for rarity | `DropConfigManager.PullCards()` logs Warning, skips that card slot | None | `Debug.LogWarning` — pull continues with fewer cards |
| `TelemetryLogger` write failure | Caught exception, logs `❌ Write failed` | None | `Debug.LogError` — telemetry row is lost silently |
| Hook session cap reached | Hook blocked with reason `"session_cap"` | N/A | `TelemetryLogger.LogHookExecution()` records block reason |
| Hook in cooldown | Hook blocked with reason `"cooldown"` | N/A | Logged via `LogHookExecution()` |
| Pack type key not in config | `DropConfigManager` logs error, returns empty card list | None | Fallback score ranges and cost 0 used in emotion system |

### 3.3 Monitoring & Logging

- **`[DropConfig]`** prefix: config load success/failure
- **`[PackOpening]`** prefix: per-pull card count and result
- **`[EmotionP2]`** prefix: per-pull quality, delta, bar values, all six bucket values (when `verbose = true`)
- **`[EmotionP2Breakdown]`** prefix: per-bucket deltas, weights, emotion labels, value score, rolling avg (when `verbose = true`)
- **`[Telemetry]`** prefix: per-pull log confirmation with duplicate/XP summary
- **`[HookOrchestrator]`** prefix: fired hooks and quiet window timestamp
- **`[Hook]`** prefix: individual hook fire/block with reason (via `LogHookExecution`)
- **CSV:** `PHASE_2_EMOTIONAL_STATE_LOG.csv` in `Application.persistentDataPath/Telemetry/csv_exports/` — primary source for analytics
- **JSON:** `pull_history.json` in same `Telemetry/` folder — full per-pull detail including Phase 2 breakdown

---

## 4. Integration Points

### 4.1 What I Emit (to other teams)

| Event/Payload | Consumer Team(s) | Contract Location | Trigger |
|---------------|------------------|-------------------|---------|
| `TelemetryLogger.OnPullLogged` (C# event: `PackPullLog`) | Any team subscribing to pull events (Analytics, LiveOps) | `TelemetryLogger.cs → public event Action<PackPullLog> OnPullLogged` | Every completed pack pull |
| `PHASE_2_EMOTIONAL_STATE_LOG.csv` | Analytics / Data team | `Application.persistentDataPath/Telemetry/csv_exports/` | Appended after every pack pull |
| `pull_history.json` | Analytics / LiveOps | `Application.persistentDataPath/Telemetry/pull_history.json` | Updated after every pack pull |
| Player XP (`player_xp` PlayerPref) | Any system reading PlayerPrefs for player progression | `PlayerPrefs key: "player_xp"` | Updated when duplicate cards grant XP |
| Player coins (`wallet_coins` PlayerPref) | Any system reading PlayerPrefs for currency | `PlayerPrefs key: "wallet_coins"` | Updated when `PlayerWallet.SpendForPack()` is called |

### 4.2 What I Consume (from other teams)

| Event/Payload | Producer Team | Contract Location | When Used |
|---------------|---------------|-------------------|-----------|
| `PlayerPrefs: "session_id"` | Session/Auth team (or self-generated) | `PlayerPrefs` | Used as telemetry session identifier in every pull log |
| `PlayerPrefs: "player_id"` | Player/Account team (or device ID) | `PlayerPrefs` | Used as player identifier in telemetry |
| `PlayerPrefs: "player_level"` | Progression/XP team (or default 1) | `PlayerPrefs` | Logged with every pull for cohort analysis |
| Card Catalog data (`cards_catalog.json`) | Design / Content team | `StreamingAssets/CCAS/cards_catalog.json` | `CardCatalogLoader` — provides the card pool for pull simulation |

### 4.3 Contract Locations

- Runtime config: `MGI_Monorepo/Assets/StreamingAssets/CCAS/phase2_config.json`
- Card catalog: `MGI_Monorepo/Assets/StreamingAssets/CCAS/cards_catalog.json`
- Telemetry output: `Application.persistentDataPath/Telemetry/` (device-local; path varies by platform)
- Reference docs: `MGI_Monorepo/Assets/Resources/CCAS/Documentation/`

---

## 5. Tunable Values & Rationale

> See [`Bhavik_Documentation.md`](Bhavik_Documentation.md) for the authoritative, submit-ready version of this section.  
> Full variable reference: [`ccas_documentation.md`](ccas_documentation.md).

*(Full Section 5 content is identical to `Bhavik_Documentation.md` — copied here for completeness.)*

### 5.1 Inventory (CCAS area)

#### Emotion Parameters

| Variable | Default | Location | Rationale | Impact of Change |
|----------|---------|----------|-----------|------------------|
| `P_max` | `3.0` | `phase2_config.json → phase_2_configuration.emotion_parameters` | Ceiling for positive emotion per pull. 3.0 allows meaningful accumulation without bar maxing out too quickly. | **Higher:** Bar fills faster, may trivialize difficult pulls. **Lower:** Satisfaction feels flat. |
| `N_max` | `2.0` | `phase2_config.json → phase_2_configuration.emotion_parameters` | Lower than `P_max` — bad pulls sting less than good pulls reward (recovery-friendly asymmetry). | **Higher:** Bad pulls feel very punishing. **Lower:** Negative emotions barely build. |
| `P_cap` / `N_cap` | `100.0` each | `phase2_config.json → phase_2_configuration.emotion_parameters` | Hard ceiling; keeps UI bars meaningful. | **Higher:** Bars rarely max out. **Lower:** Emotions saturate quickly. |

#### Pack-Type Quality Bias

| Variable | Default | Location | Rationale | Impact of Change |
|----------|---------|----------|-----------|------------------|
| Bronze bias exponent | `0.8` | `EmotionalStateManager.cs` | Optimistic bias for lowest-tier pack; keeps casual players engaged. | **Closer to 1.0:** bronze feels neutral. |
| Silver bias exponent | `1.0` | `EmotionalStateManager.cs` | Neutral reference. | Shifting affects all mid-tier comparisons. |
| Gold bias exponent | `1.2` | `EmotionalStateManager.cs` | Stricter — premium expectation. | **Higher:** Gold very punishing for average pulls. |

#### Routing Thresholds

| Variable | Default | Location | Rationale | Impact of Change |
|----------|---------|----------|-----------|------------------|
| `quality_good_threshold` | `0.62` | Config routing | Above-average pull required for positive routing. | **Higher:** Fewer triggers. **Lower:** Too generous. |
| `quality_peak_threshold` | `0.85` | Config routing | Maximum rarity routing weight (Thrill reserved for exceptional pulls). | **Higher:** Rarer peaks. **Lower:** Dilutes Thrill. |
| `quality_bad_threshold` | `0.38` | Config routing | Negative rarity routing activates below this. | **Higher:** More punishing. **Lower:** More forgiving. |
| `streak_window` | `5` | Config routing | Rolling window for cold/hot mood detection. | **Higher:** Slower mood change. **Lower:** Volatile. |
| `cold_streak_threshold` | `0.40` | Config routing | Avg quality below this = cold mood (Relief routing). | **Higher:** Cold mood more common. **Lower:** Requires very bad run. |
| `hot_streak_threshold` | `0.60` | Config routing | Avg quality above this = hot mood (Letdown routing). | **Higher:** Hot mood rarer. **Lower:** Frequent Letdowns. |
| `value_score_scale` | `1000.0` | Config routing | Makes value scores human-readable (~1–5 range). | Scale thresholds proportionally if changed. |
| `value_good_threshold` | `2.20` | Config routing | Good value-for-cost triggers Worth positive routing. | **Higher:** More selective economy trigger. **Lower:** Economy emotion too common. |
| `value_bad_threshold` | `1.80` | Config routing | Bad value-for-cost triggers Regret (high-cost packs only). | **Higher:** Regret fires more. **Lower:** Wider neutral zone. |
| `high_cost_threshold_coins` | `1500` | Config routing | Minimum cost to qualify for economy-regret routing. | **Higher:** Only Gold triggers Regret. **Lower:** Bronze can trigger Regret. |

#### Decay

| Variable | Default | Location | Rationale | Impact of Change |
|----------|---------|----------|-----------|------------------|
| Pos/Neg `rarity_pack` decay | `0.985` | Config decay | Slow fade (~1.5%/pull) — Thrill/Disappointment linger. | **Lower:** Fast fade. **→1.0:** Never fades. |
| Pos/Neg `streak` decay | `0.920` | Config decay | Fast fade (~8%/pull) — Relief/Letdown are momentary. | **Higher:** Streak emotions persist past their context. |
| Pos/Neg `economy` decay | `0.960` | Config decay | Moderate fade (~4%/pull) — Worth/Regret fade over a few pulls. | **Lower:** Purely momentary. **Higher:** Lasting session shadow. |

#### Recovery

| Variable | Default | Location | Rationale | Impact of Change |
|----------|---------|----------|-----------|------------------|
| `good_pull_reduces_negative` | `0.50` | Config recovery | Good pulls push back the negative bar at 50% of positive delta. | **Higher:** Very forgiving. **Lower:** Negative lingers. |
| `bad_pull_reduces_positive` | `0.50` | Config recovery | Bad pulls dent the positive bar at 50% of negative delta. | **Higher:** Positive is fragile. **Lower:** Positive bar is sticky. |
| `recovery.enabled` | `true` | Config recovery | Master toggle. | **Disabled:** Bars become independent — breaks tug-of-war design. |

#### Duplicate XP

| Variable | Default | Location | Rationale | Impact of Change |
|----------|---------|----------|-----------|------------------|
| `common_duplicate_xp` | `5` | `phase2_config.json → duplicate_xp` | Minimal to not incentivize farming commons. | **Higher:** Commons become XP-farming targets. |
| `uncommon_duplicate_xp` | `10` | Config duplicate_xp | 2× common. | Shifts Uncommon-heavy strategy value. |
| `rare_duplicate_xp` | `25` | Config duplicate_xp | 5× common — softens Rare duplicate disappointment. | **Higher:** Rare dupes worth chasing. |
| `epic_duplicate_xp` | `50` | Config duplicate_xp | 10× common. | **Lower:** Epic dupes feel purely wasteful. |
| `legendary_duplicate_xp` | `100` | Config duplicate_xp | 20× common — max consolation for wasted legendary slot. | **Lower:** Highest player dissatisfaction risk. |

### 5.2 Location Legend

- **Config:** `StreamingAssets/CCAS/phase2_config.json`
- **File:** Hardcoded in C# source (requires code change + recompile)
- **Env:** Environment variable
- **DB:** Database / remote config

### 5.3 Change Impact Summary

- **Emotion feel:** `P_max`, `N_max`, pack bias exponents → primary levers
- **Routing behavior:** Quality/streak/economy thresholds → which actions count emotionally
- **Session arc:** Decay + recovery → how emotions evolve across a session
- **Economy dimension:** Value score thresholds + `high_cost_threshold_coins` → cost-vs-reward feeling
- **Duplicate fairness:** Duplicate XP → progression/economy lever (no effect on emotional bars)

---

## 6. Documentation Status

| Section | Status |
|---------|--------|
| Code ownership | ☑ Done — all 15+ files mapped with purpose and entry points |
| Schemas/contracts | ☑ Done — JSON shapes, CSV columns, validation rules, versioning |
| Workflows | ☑ Done — full pack-open flow, UI update loop, telemetry, config load, session reset |
| Integration (emit) | ☑ Done — `OnPullLogged` event, CSV, JSON, PlayerPrefs keys |
| Integration (consume) | ☑ Partial — PlayerPrefs sources noted; external team contracts (Analytics, session/auth) TBD |
| Tunable values | ☑ Done — all variables from config + hardcoded constants with rationale and impact |

**Handoff notes:** See `documentation_status.md` for a detailed breakdown of what was completed from source code alone vs. what requires additional team input (cross-team contracts, remote server-side config, scene hierarchy details).

---

## Quick Links

- [Workflow Examples (Streamlit)](https://mgi-system-diagram.streamlit.app/)
- [DocMost - Standardized Doc](link TBD)
- [Contract Repository](path TBD)

---

## Appendix: Template Usage

1. **One file per team member** — e.g. `TEAM_Acquisition_JohnDoe.md` or `Acquisition_JohnDoe.md`
2. New hire: copy template → rename → fill in your areas
3. Offboarding: hand off your file to successor; they inherit and update
4. Align to the integration team's finalized standardized doc once posted
5. Update on meaningful changes; keep "Last Updated" current
