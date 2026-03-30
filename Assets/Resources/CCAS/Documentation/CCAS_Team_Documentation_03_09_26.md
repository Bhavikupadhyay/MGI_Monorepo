# Team Documentation — CCAS

---

## Owner Overview

| Field | Value |
|-------|-------|
| **Owner** | Bhavik Upadhyay, Vrushali Ghotavadekar |
| **Team** | CCAS |
| **Last Updated** | 2026-03-09 |

---

## 1. Code Ownership & File-Level Notes

### 1.1 Module Map

| Module/File | Purpose | Entry Points |
|-------------|---------|--------------|
| `Assets/Scripts/CCAS/EmotionalStateManager.cs` | Singleton MonoBehaviour — Phase 2 emotional state engine. Computes positive/negative deltas from pull quality, routes into three buckets per family (rarity_pack, streak, economy), applies decay and recovery, recomputes family levels (0–100) | `ApplyPackOutcome(packTypeKey, rarities)`, `Snapshot()`, `ResetSession()` |
| `Assets/Scripts/CCAS/PackOpeningController.cs` | MonoBehaviour — opens a pack by key: pulls cards, updates emotions and hooks, logs the pull, displays cards via CardView | `OpenPackOfType(key)`, `OpenPack()` |
| `Assets/Scripts/CCAS/DropConfigManager.cs` | Singleton MonoBehaviour — loads `phase2_config.json` at startup; performs weighted card pulls by rarity or full card objects | `PullCards(packKey)`, `PullCardRarities(packKey)` |
| `Assets/Scripts/CCAS/EmotionDisplayUI.cs` | MonoBehaviour — renders positive/negative bars (0–100), labels, and short-lived emotion popups (Thrill, Relief, Worth, Disappointment, Letdown, Regret) | `Start()`, `Update()`, `TrySpawnPopups()` |
| `Assets/Scripts/CCAS/HookOrchestrator.cs` | Singleton MonoBehaviour — gates outcome hooks with a global quiet window, per-hook cooldowns, and per-session caps | `TryFireHook(hookId, cooldown, cap, payload, out reason)`, `TryTriggerOutcomeHooks(rarities)`, `ResetHooks()` |
| `Assets/Scripts/CCAS/TelemetryLogger.cs` | Singleton MonoBehaviour — logs every pull to `pull_history.json`, detects duplicates, assigns duplicate XP, exports Phase 2 emotional state to CSV, fires `OnPullLogged` | `LogPull(packTypeKey, packName, cost, cards)`, `GetRecent(count)`, `ClearLogFile()` |
| `Assets/Scripts/CCAS/PlayerWallet.cs` | Singleton MonoBehaviour — manages player coins; affordability check, spend, add; persists to PlayerPrefs; raises `OnChanged` | `CanAfford(pack)`, `SpendForPack(pack)`, `AddCoins(amount)` |
| `Assets/Scripts/CCAS/AcquisitionHubController.cs` | MonoBehaviour — central controller for the acquisition flow; ensures only one panel is visible at a time (Hub, Market, Pack Opening, Drop History, My Packs); updates coins/XP display | `ShowHub()`, `ShowMarket()`, `ShowPackOpening(packKey)`, `ShowHistory()` |
| `Assets/Scripts/CCAS/BoosterMarketAuto.cs` | MonoBehaviour — builds Booster Market UI from config; one button per pack type; clicking calls hub to open that pack | `Start()` → `GeneratePackButtons()`, `TryOpenPack(packKey)` |
| `Assets/Scripts/CCAS/CardCatalogLoader.cs` | Singleton MonoBehaviour — loads `cards_catalog.json` at startup; builds tier index; provides random card lookups by tier or rarity | `GetRandomCardByTier(tier)`, `GetRandomCardByRarity(rarity)`, `GetCardsByTier(tier)` |
| `Assets/Scripts/CCAS/DropHistoryController.cs` | MonoBehaviour — powers Drop History panel; lists recent pulls with rarity colors, duplicate + XP info; shows latest positive/negative values | `RefreshDropHistory()` (also triggered by `TelemetryLogger.OnPullLogged`) |
| `Assets/Scripts/CCAS/CardView.cs` | MonoBehaviour — visual representation of a single card slot: rarity color, text (name, team, element, position), optional fade-in | `Apply(Card card)`, `Apply(string rarityLower)` |
| `Assets/Scripts/CCAS/config/CCASConfigLoader.cs` | Static class — generic utility to load any JSON config from `StreamingAssets/CCAS/` and deserialize to type T | `Load<T>(fileName)` |
| `Assets/Scripts/CCAS/Card.cs` | Data classes — Card (uid, cardTier, name, team, element, position5) and CardsCatalog | `GetRarityString()`, `Card.RarityStringToTier(rarity)` |
| `Assets/Scripts/CCAS/DropConfigModels.cs` | Data classes — all types for deserializing `phase2_config.json` (CCASConfigRoot, Phase2Configuration, PackType, DropRates, DuplicateXP, etc.) | N/A (data model) |
| `Assets/Scripts/CCAS/RarityColorUtility.cs` | Static class — single source for rarity to Color32 mapping | `GetColorForRarity(rarity)` |
| `Assets/StreamingAssets/CCAS/phase2_config.json` | Primary runtime config — pack types, rarity values, emotion tuning, duplicate XP. Loaded by DropConfigManager on Awake | Loaded at runtime |
| `Assets/StreamingAssets/CCAS/cards_catalog.json` | Full card catalog (uid, cardTier, name, team, element, position5). Loaded by CardCatalogLoader on Awake | Loaded at runtime |

### 1.2 Key Flows

| Flow | Trigger | Steps | Exit/Handoff |
|------|---------|-------|--------------|
| Pack open | User taps a pack button | 1. BoosterMarketAuto → AcquisitionHubController.ShowPackOpening(packKey) → 2. PackOpeningController.OpenPackOfType(key) → 3. DropConfigManager.PullCards(packType) → 4. Extract rarities from cards → 5. EmotionalStateManager.ApplyPackOutcome(packType, rarities) → 6. HookOrchestrator.TryTriggerOutcomeHooks(rarities) → 7. TelemetryLogger.LogPull(...) → 8. CardView.Apply(card) per card → 9. packPanel active, dropHistoryPanel hidden, layout rebuilt | Continue button → AcquisitionHubController.ShowHistory() + DropHistoryController.RefreshDropHistory() |
| Emotional state update | Inside ApplyPackOutcome | 1. Compute raw score from rarity values → 2. Normalize to quality01 using pack score range → 3. Apply pack-type bias exponent → 4. Compute dP/dN from asymmetric curves → 5. Apply optional rare boost on dP → 6. Apply bucket decay → 7. Route dP/dN into rarity_pack/streak/economy buckets → 8. Apply recovery → 9. Recompute positive and negative from weighted buckets | Returns EmotionDeltaResult; EmotionDisplayUI reads via Snapshot() and GetLastBreakdown() |
| Startup data loading | Scene Awake | 1. DropConfigManager.Awake loads phase2_config.json → 2. CardCatalogLoader.Awake loads cards_catalog.json and builds tier index | All systems ready before first pack open |
| Duplicate XP | Inside TelemetryLogger.LogPull — runs after emotions are finalized | 1. BuildCardPullCountsFromHistory() checks if UID seen before → 2. If duplicate, GetDuplicateXpForRarity(rarity) from config.duplicate_xp → 3. Add to player_xp in PlayerPrefs | XP total saved to PlayerPrefs; AcquisitionHubController reads for display. Emotion values are NOT affected by duplicate status |
| Telemetry export | After every LogPull | 1. Append PackPullLog to in-memory cache → 2. Trim to MaxLogs → 3. Save pull_history.json → 4. Append row to PHASE_2_EMOTIONAL_STATE_LOG.csv → 5. Fire OnPullLogged | DropHistoryController refreshes on OnPullLogged |

### 1.3 Dependencies

- **Internal:** PackOpeningController → DropConfigManager, EmotionalStateManager, HookOrchestrator, TelemetryLogger, CardView; EmotionDisplayUI → EmotionalStateManager; TelemetryLogger → DropConfigManager (duplicate_xp), EmotionalStateManager (snapshot + breakdown); BoosterMarketAuto → DropConfigManager (pack_types); DropHistoryController → TelemetryLogger, RarityColorUtility
- **External:** Unity engine; Newtonsoft.Json; TextMeshPro; Application.streamingAssetsPath (config/catalog); Application.persistentDataPath (telemetry output); PlayerPrefs (coins, XP)
- **Cross-team:** Consumes current_balance, transaction_status, new_balance from Economy; consumes player_id/team_id from Progression; emits spend payload to Economy; emits duplicate XP events to Progression

### 1.4 Gotchas

- **Bias exponents and curve exponents are hardcoded** in EmotionalStateManager.cs (Bronze 0.8, Silver 1.0, Gold 1.2; positive curve 0.7, negative curve 1.2) — not in `phase2_config.json`. Changes require a code deploy.
- **HookOrchestrator values are also hardcoded** — quiet window, cooldowns, and session caps are not in config.
- **Duplicate check happens AFTER emotion calculation** — Steps 1–6 of the pipeline run identically whether cards are duplicates or not. Duplicate status affects only XP gain and collection updates, never emotion values.
- **No purchase flow wired yet** — BoosterMarketAuto opens packs immediately without going through Economy. PlayerWallet.SpendForPack exists but Economy integration is not yet connected. This is a known next step.
- **Coins display is hardcoded** — AcquisitionHubController shows `"Coins: 1200"` as a hardcoded string in Start(). It does NOT read from PlayerWallet. PlayerWallet starting balance is 500 (inspector default). These two values are out of sync and both need to be reconciled when the purchase flow is wired.
- **My Packs is disabled** — `myPacksButton.interactable = false` in AcquisitionHubController.
- **ResetSession() is called on Awake()** — emotion state (positive/negative and all buckets) resets every time the scene loads, not just when explicitly called. This is expected behaviour but easy to miss when testing across scene transitions.
- **ResetSession() clears emotion only** — resets positive/negative and all buckets to 0 and clears the quality window, but does NOT reset XP or pull history. To reset those separately use TelemetryLogger.ClearLogFile() and clear player_xp in PlayerPrefs.
- **Rolling quality window uses previous pulls only** — streak mood (cold/hot) for the current pull is computed from the window before the current pull is enqueued. The window is updated at the end of ApplyPackOutcome after all deltas and recovery are applied.
- **Telemetry has a MaxLogs cap of 1000 and MaxFileSizeKB cap of 512KB** — when either limit is hit, oldest logs are trimmed. If the file exceeds 512KB and there are more than 100 logs, 100 entries are removed. Old logs can disappear silently; be aware when debugging long sessions.
- **LogHookExecution does not write to any file** — it only calls Debug.Log when verboseLogging = true. It is debug-only and produces no persistent record.
- **phase1_config.json is not loaded** by current code — DropConfigManager only loads phase2_config.json. Phase 1 is kept for reference only. Note: the LoadConfig() comment in DropConfigManager still says "Phase 1" — this is a stale comment in the code, not a behaviour difference.
- **Everything under Assets/Resources/CCAS/** is not loaded at runtime — design/reference only.
- **Telemetry files are outside the repo** — written to `Application.persistentDataPath/Telemetry/`. macOS: `~/Library/Application Support/DefaultCompany/MGI_Monorepo/Telemetry/`. Windows: `%USERPROFILE%\AppData\LocalLow\DefaultCompany\MGI_Monorepo\Telemetry\`. Easy to miss when debugging.
- **value_score_scale** (1000.0) must be adjusted proportionally if coin cost values are ever rescaled.
- **recovery.enabled = false** makes positive/negative bars fully independent — disables the tug-of-war mechanic entirely.

---

## 2. Schemas & Contracts

### 2.1 JSON Schemas

| Schema Name | Version | Location | Description |
|-------------|---------|----------|-------------|
| `phase2_config` | `ccas.p2_emotion_families.v1` | `StreamingAssets/CCAS/phase2_config.json` | Master runtime config for CCAS Phase 2. Contains pack types, rarity values, Phase 2 emotion configuration (parameters, families, routing, decay, recovery), and duplicate XP. Loaded by DropConfigManager on Awake. |
| `cards_catalog` | N/A | `StreamingAssets/CCAS/cards_catalog.json` | Full card catalog array. Each entry: uid, cardTier (1–5), name, team, element, position5. Loaded by CardCatalogLoader on Awake. |
| `pull_history` | N/A | `Application.persistentDataPath/Telemetry/pull_history.json` | Runtime telemetry log. Each entry is a PackPullLog: event_id, timestamp, session_id, player_id, pack_type, cards, duplicates, XP, emotion snapshot, Phase 2 breakdown. Written by TelemetryLogger; not in repo. |
| `PHASE_2_EMOTIONAL_STATE_LOG` | N/A | `Application.persistentDataPath/Telemetry/csv_exports/` | CSV export of Phase 2 emotional state per pull. Columns: log_id, timestamp, session_id, player_id, event_type, negative_after, positive_after, negative_delta, positive_delta, pos/neg deltas per bucket. Written by TelemetryLogger; not in repo. |
| `phase1_config` | N/A | `StreamingAssets/CCAS/phase1_config.json` | Legacy Phase 1 schema (satisfaction/frustration, emotion_dynamics). Not loaded by current code; kept for reference only. |

### 2.2 Payload Shapes

**Config root keys (CCASConfigRoot):**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `schema_version` | string | Yes | e.g. `ccas.p2_emotion_families.v1` |
| `methodology` | string | Yes | Descriptor for the config methodology |
| `rarity_values` | object | Yes | Maps rarity name to numeric_value and display_name (Common=1 … Legendary=5) |
| `pack_types` | object | Yes | Keyed by pack name; each entry has name, cost, guaranteed_cards, drop_rates, score_range |
| `phase_2_configuration` | object | Yes | Emotion parameters, families, routing, decay, recovery |
| `duplicate_xp` | object | Yes | XP per duplicate card by rarity tier (common through legendary) |

**phase_2_configuration sub-fields:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `emotion_parameters` | object | Yes | P_max, N_max, P_cap, N_cap |
| `families` | object | Yes | positive and negative buckets and weights (rarity_pack, streak, economy) |
| `routing` | object | Yes | quality_good_threshold, quality_peak_threshold, quality_bad_threshold, high_cost_threshold_coins, streak_window, cold_streak_threshold, hot_streak_threshold, value_score_scale, value_good_threshold, value_bad_threshold |
| `decay` | object | Yes | positive and negative sub-objects each with rarity_pack, streak, economy multipliers |
| `recovery` | object | Yes | enabled (bool), good_pull_reduces_negative, bad_pull_reduces_positive |

**pack_types entry fields:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | Yes | Display name of the pack |
| `cost` | int | Yes | Cost in coins (e.g. 1000, 1500, 2000) |
| `guaranteed_cards` | int | Yes | Number of cards per pack open (currently 3 for all packs) |
| `drop_rates` | object | Yes | common, uncommon, rare, epic, legendary (floats, sum to 1) |
| `score_range` | object | Yes | min_score, max_score — used to normalize quality01 |

**Phase2PullBreakdown (emitted per pull to telemetry and UI):**

| Field | Description |
|-------|-------------|
| `pack_type`, `raw_score`, `quality01`, `cost_coins` | Pull context |
| `has_rare_or_better`, `max_rarity_numeric` | Rarity context |
| `quality_avg_window`, `cold_mood`, `hot_mood`, `value_score` | Streak and economy context |
| `pos_emotions`, `neg_emotions` | Emotion labels fired (e.g. "Thrill, Relief", "Disappointment") |
| `pos_d_rarity_pack`, `pos_d_streak`, `pos_d_economy` | Positive deltas per bucket |
| `neg_d_rarity_pack`, `neg_d_streak`, `neg_d_economy` | Negative deltas per bucket |
| `pos_w_*`, `neg_w_*` | Routing weights per bucket (for debugging) |
| `applied_positive_total`, `applied_negative_total` | Total delta applied to each family |
| `positive_after`, `negative_after` | Final bar values after the pull |

**CSV export columns (PHASE_2_EMOTIONAL_STATE_LOG.csv):**

| Columns |
|---------|
| log_id, timestamp, session_id, player_id, event_type, negative_after, positive_after, negative_delta, positive_delta, pos_d_rarity_pack, pos_d_streak, pos_d_economy, neg_d_rarity_pack, neg_d_streak, neg_d_economy |

### 2.3 Validation Rules

| Rule | Detail |
|------|--------|
| Rarity values | Must be positive integers; Common=1, Uncommon=2, Rare=3, Epic=4, Legendary=5 |
| Score ranges | min_score < max_score; used in clamp(rawQuality, 0, 1) |
| Drop rates | common + uncommon + rare + epic + legendary should sum to 1.0 per pack |
| Decay values | Must be in (0, 1]; applied as multipliers each pull |
| Family weights | rarity_pack + streak + economy weights should sum to 1.0 per family |
| `recovery.enabled` | Must be boolean |
| Quality thresholds | bad_threshold < good_threshold < peak_threshold (e.g. 0.38 < 0.62 < 0.85) |
| Missing config | If phase_2_configuration or nested blocks are missing, hardcoded fallbacks are used: P_max=3, N_max=2, decay 0.985/0.92/0.96, family weights 0.5/0.3/0.2 |
| Unknown pack key | Matched case-insensitively and by substring; unknown pack types use Silver-like fallbacks and Epic+ as special rarity default |
| Duplicate XP | Calculated after emotion steps 1–6 are finalized; duplicate status never affects emotion values |

### 2.4 Versioning Assumptions

| Rule | Detail |
|------|--------|
| Current version | `ccas.p2_emotion_families.v1` |
| Non-breaking changes | Value tuning (thresholds, decay, weights), adding new optional fields — no version increment required |
| Breaking changes | Renamed fields, structural changes, new required fields — require version bump and migration |
| Phase 1 config | `phase1_config.json` exists in StreamingAssets but is not loaded by current code; kept for reference only |

---

## 3. Workflows / Pipelines

### 3.1 End-to-End Flow

```
Pack key (e.g. "bronze_pack")
  → DropConfigManager.PullCards(packKey)
      → WeightedRoll(drop_rates) per card slot
      → CardCatalogLoader.GetRandomCardByRarity(rarity) → List<Card>
  → EmotionalStateManager.ApplyPackOutcome(packKey, rarities)
      → Compute raw score from rarity values
      → Normalize to quality01 using pack score range
      → Apply pack-type bias exponent (Bronze 0.8 / Silver 1.0 / Gold 1.2)
      → Compute dP / dN from asymmetric curves
      → Optional rare boost on dP
      → Apply bucket decay (rarity_pack / streak / economy)
      → Route dP / dN into buckets by quality thresholds, streak mood, value score
      → Apply recovery (cross-family dampening)
      → Recompute positive / negative from weighted buckets
      → EmotionDisplayUI reads Snapshot() + GetLastBreakdown() → bars + popups
  → HookOrchestrator.TryTriggerOutcomeHooks(rarities)
      → outcome_streak hook if not blocked by quiet window / cooldown / session cap
  → TelemetryLogger.LogPull(packKey, packName, cost, cards)
      → Duplicate detection via BuildCardPullCountsFromHistory()
      → Duplicate XP from config.duplicate_xp → PlayerPrefs "player_xp"
        (NOTE: runs AFTER emotion is finalized; duplicate status does not affect emotion)
      → Append to pull_history.json
      → Append row to PHASE_2_EMOTIONAL_STATE_LOG.csv
      → Fire OnPullLogged → DropHistoryController.RefreshDropHistory()
  → PackOpeningController: CardView.Apply(card) per card → pack panel UI
  → Continue button → ShowHistory() → DropHistoryController refreshes
        ↓ failure (no cards returned)?
            → Fallback to PullCardRarities() → rarity-only display (no CardView)
```

### 3.2 Failure Handling

| Failure Type | Behavior | Retries | Escalation |
|--------------|----------|---------|------------|
| Config file missing at startup | DropConfigManager uses hardcoded fallbacks (P_max=3, N_max=2, decay 0.985/0.92/0.96, weights 0.5/0.3/0.2) | No | Log warning; game continues with degraded state |
| Invalid config value | Clamp or skip affected parameter | No | Log error; flag for review |
| PullCards returns empty list | PackOpeningController falls back to PullCardRarities + rarity-only display | No | No cards shown with full detail; rarity color display only |
| Empty or null rarities list | Treated as empty list; rawScore=0, quality goes to 0 or low; negative delta may apply | No | Silent; emotion still calculated |
| Unknown pack key | Matched by substring; falls back to Silver-like defaults, cost=0, valueScore=0 | No | Log warning; pack opens with fallback values |
| All routing weights = 0 | No delta applied that pull; bars can still move via recovery | No | Silent; expected behaviour in edge cases |
| Hook blocked | HookOrchestrator returns false with blockReason | No | TelemetryLogger.LogHookExecution records block reason (debug only) |
| Telemetry write failure | Pull proceeds; log loss only | No | Silent; no impact on gameplay |

### 3.3 Monitoring & Logging

- Every pull written to `pull_history.json` (`Application.persistentDataPath/Telemetry/`) — full event: cards, duplicates, XP, emotion snapshot, Phase 2 breakdown
- Phase 2 emotional state appended per pull to `PHASE_2_EMOTIONAL_STATE_LOG.csv` (`/Telemetry/csv_exports/`) — primary source for tuning analysis
- Hook fires and blocks logged via `TelemetryLogger.LogHookExecution(hookId, fired, reasonIfBlocked, context)` (debug level)
- `OnPullLogged` event fires after every successful log — consumed by DropHistoryController to refresh the history panel
- **Telemetry paths (not in repo — easy to miss):**
  - macOS: `~/Library/Application Support/DefaultCompany/MGI_Monorepo/Telemetry/`
  - Windows: `%USERPROFILE%\AppData\LocalLow\DefaultCompany\MGI_Monorepo\Telemetry\`
- Monitor positive/negative bar values in the CSV for saturation (frequently hitting P_cap/N_cap of 100.0)
- Full workflow flowcharts (Mermaid): `Assets/Resources/CCAS/reference/CCAS_Workflow_Flowchart.md`

---

## 4. Integration Points

### 4.1 What I Emit (to other teams)

| Event/Payload | Consumer Team(s) | Contract Location | Trigger |
|---------------|------------------|-------------------|---------|
| player_id, type="spend", currency="coins", amount, source="pack_purchase", timestamp, wallet_before, wallet_after, session_total_spent | Economy | TBD | Pack purchase |
| player_id, team_id, xp_amount, xp_type="collection_duplicate", xp_event_id, timestamp, xp_before, xp_after | Progression | TBD | Duplicate card pulled from pack |

### 4.2 What I Consume (from other teams)

| Event/Payload | Producer Team | Contract Location | When Used |
|---------------|---------------|-------------------|-----------|
| current_balance (coins) | Economy | TBD | Validate player can buy pack; display in UI |
| transaction_status (success/failure) | Economy | TBD | Validate pack purchase before opening |
| new_balance | Economy | TBD | Update UI wallet state after spend |
| player_id / team_id (canonical format) | Progression | TBD | Correlate CCAS pulls to correct team/player |

### 4.3 Contract Locations

- Economy dependency contract: CCAS — Economy Dependency Table (PDF)
- Progression dependency contract: CCAS — Progression Dependency Table (PDF)
- Shared contracts: TBD — align with integration team standardized doc

---

## 5. Tunable Values & Rationale

> Config file: `StreamingAssets/CCAS/phase2_config.json`
> Schema Version: `ccas.p2_emotion_families.v1`

### 5.1 Inventory (CCAS area)

#### 5.1.1 Rarity Values
Defined in `phase2_config.json → rarity_values`. Used to compute the raw score from a pack pull. Each card's rarity maps to a numeric point value.

| Variable | Default | Location | Rationale | Impact of Change |
|----------|---------|----------|-----------|------------------|
| `common` rarity value | 1 | `phase2_config.json` | Baseline card value | **Higher:** Inflates all pack scores |
| `uncommon` rarity value | 2 | `phase2_config.json` | 2x common | **Higher:** Uncommons carry more weight |
| `rare` rarity value | 3 | `phase2_config.json` | 3x common | **Higher:** Rares drive scores up faster |
| `epic` rarity value | 4 | `phase2_config.json` | 4x common | **Higher:** Epics dominate pack quality |
| `legendary` rarity value | 5 | `phase2_config.json` | 5x common | **Higher:** Legendaries guarantee max quality |

#### 5.1.2 Pack Types
Defined in `phase2_config.json → pack_types`. Each pack is a keyed entry with sub-fields that control cost, yield, and emotional scoring boundaries. Drop rates and score ranges establish the baseline expected quality for each pack. Elite and Supreme are planned next steps — see Section 6 Handoff Notes.

| Variable | Default | Location | Rationale | Impact of Change |
|----------|---------|----------|-----------|------------------|
| Bronze Score Range | [3, 7] | `phase2_config.json` | Max/Min score bounds for Bronze | Shifts quality calculation baseline |
| Silver Score Range | [6, 12] | `phase2_config.json` | Max/Min score bounds for Silver | Shifts quality calculation baseline |
| Gold Score Range | [9, 13] | `phase2_config.json` | Max/Min score bounds for Gold | Shifts quality calculation baseline |
| Elite Score Range | TBD | `phase2_config.json` | To be added | TBD |
| Supreme Score Range | TBD | `phase2_config.json` | To be added | TBD |

#### 5.1.3 Emotion Parameters
Defined in `phase2_config.json → phase_2_configuration.emotion_parameters`. Controls the magnitude of emotional deltas each pull can produce.

| Variable | Default | Location | Rationale | Impact of Change |
|----------|---------|----------|-----------|------------------|
| `P_max` | 3.0 | `phase2_config.json` | Ceiling for positive emotion per pull | **Higher:** Bar fills faster, may trivialize difficult pulls |
| `N_max` | 2.0 | `phase2_config.json` | Ceiling for negative emotion per pull; asymmetrical to favor recovery | **Higher:** Bad pulls feel much more punishing |
| `P_cap` / `N_cap` | 100.0 | `phase2_config.json` | Hard ceiling limits to keep UI bars meaningful | **Lower:** Emotions saturate quickly, reducing granularity |
| Positive curve exponent | 0.7 | `EmotionalStateManager.cs` | `pow(quality01, 0.7)` | Flattens or steepens positive acquisition |
| Negative curve exponent | 1.2 | `EmotionalStateManager.cs` | `pow(1−quality01, 1.2)` | Flattens or steepens negative acquisition |
| Rare boost multiplier | 1.15 | `EmotionalStateManager.cs` | `dP` boost when Rare/Epic/Legendary present | Modifies impact of pulling high rarity cards |

#### 5.1.4 Phase 2 Family Weights
Defined in `phase2_config.json → phase_2_configuration.families`. The three bucket values are combined using these normalized weights to produce the final `positive` and `negative` bar values.

| Variable | Default | Location | Rationale | Impact of Change |
|----------|---------|----------|-----------|------------------|
| Default `positive` weights | 0.5/0.3/0.2 | `phase2_config.json` | rarity_pack / streak / economy weights | Changes primary driver of positive emotion |
| Default `negative` weights | 0.5/0.3/0.2 | `phase2_config.json` | rarity_pack / streak / economy weights | Changes primary driver of negative emotion |

#### 5.1.5 Routing Thresholds
Defined in `phase2_config.json → phase_2_configuration.routing`. These thresholds decide which emotional buckets participate per pull and how strongly.

| Variable | Default | Location | Rationale | Impact of Change |
|----------|---------|----------|-----------|------------------|
| Bronze bias exponent | 0.8 | `EmotionalStateManager.cs` | Optimistic quality curve to keep casual players engaged | **Closer to 1.0:** Bronze feels more neutral/punishing |
| Silver bias exponent | 1.0 | `EmotionalStateManager.cs` | Neutral baseline expectation | Shifts emotional baseline for mid-tier |
| Gold bias exponent | 1.2 | `EmotionalStateManager.cs` | Stricter expectation for premium packs | **Higher:** Gold feels very punishing for average pulls |
| `quality_good_threshold` | 0.62 | `phase2_config.json` | Boundary for "good" pull positive routing | **Higher:** Fewer pulls trigger positive routing |
| `quality_peak_threshold` | 0.85 | `phase2_config.json` | Triggers maximum rarity/pack routing weight | **Higher:** Peak moments (Thrill) are rarer |
| `quality_bad_threshold` | 0.38 | `phase2_config.json` | Activates negative rarity routing for bad pulls | **Higher:** More punishing; more pulls feel disappointing |
| `streak_window` | 5 | `phase2_config.json` | Rolling average length for cold/hot mood | **Lower:** Mood reacts immediately; volatile |
| `cold_streak_threshold` | 0.40 | `phase2_config.json` | quality < 0.40 = cold mood (Relief routing) | **Higher:** Cold mood triggers more easily |
| `hot_streak_threshold` | 0.60 | `phase2_config.json` | quality > 0.60 = hot mood (Letdown routing) | **Lower:** Frequent Letdowns on average bad pulls |
| `value_score_scale` | 1000.0 | `phase2_config.json` | Multiplier to make value scores human-readable | Needs scaling adjustments if modified |
| `value_good_threshold` | 2.20 | `phase2_config.json` | Activates Worth/economy positive routing | **Lower:** Economy positive is too common |
| `value_bad_threshold` | 1.80 | `phase2_config.json` | Activates Regret/negative economy routing | **Higher:** Regret fires more often |
| `high_cost_threshold_coins` | 1500 | `phase2_config.json` | Min cost to trigger economy-regret routing | **Lower:** Cheap packs can trigger Regret |

#### 5.1.6 Phase 2 Decay Parameters
Defined in `phase2_config.json → phase_2_configuration.decay`. Applied **before** new deltas each pull. Lower value = faster fade.

| Variable | Default | Location | Rationale | Impact of Change |
|----------|---------|----------|-----------|------------------|
| Pos/Neg `rarity_pack` decay | 0.985 | `phase2_config.json` | Thrill/Disappointment linger (~1.5% fade) | **Lower:** Fast fade; emotion is gone in a pull or two |
| Pos/Neg `streak` decay | 0.920 | `phase2_config.json` | Relief/Letdown are momentary (~8% fade) | **Higher:** Streak emotions persist past context |
| Pos/Neg `economy` decay | 0.960 | `phase2_config.json` | Worth/Regret fade moderately (~4% fade) | **Higher:** Lasting session shadow |

#### 5.1.7 Phase 2 Recovery Parameters
Defined in `phase2_config.json → phase_2_configuration.recovery`. Controls cross-family dampening — good pulls reduce the negative bar and bad pulls reduce the positive bar.

| Variable | Default | Location | Rationale | Impact of Change |
|----------|---------|----------|-----------|------------------|
| `good_pull_reduces_negative` | 0.50 | `phase2_config.json` | Good pulls push back on negative bar (50%) | **Higher:** Very forgiving; negative vanishes quickly |
| `bad_pull_reduces_positive` | 0.50 | `phase2_config.json` | Bad pulls dent positive bar (50%) | **Lower:** Positive bar is sticky/impervious |
| `recovery.enabled` | true | `phase2_config.json` | Master toggle for cross-family dampening | **Disabled:** Bars independent; breaks tug-of-war |

#### 5.1.8 Duplicate XP
Defined in `phase2_config.json → duplicate_xp`. Controls how many XP points a player receives when opening a duplicate card. Calculated after emotion steps 1–6 are finalized — duplicate status never affects emotion values.

| Variable | Default | Location | Rationale | Impact of Change |
|----------|---------|----------|-----------|------------------|
| `common_duplicate_xp` | 5 | `phase2_config.json` | Minimal reward; discourages farming | **Higher:** Farming commons becomes viable |
| `uncommon_duplicate_xp` | 10 | `phase2_config.json` | 2x common reward | Shifts relative value of uncommons |
| `rare_duplicate_xp` | 25 | `phase2_config.json` | 5x common reward | **Higher:** Rare dupes become desirable |
| `epic_duplicate_xp` | 50 | `phase2_config.json` | 10x common reward | **Lower:** Epic dupes feel wasteful |
| `legendary_duplicate_xp` | 100 | `phase2_config.json` | 20x common reward | **Lower:** Highest player dissatisfaction risk |

### 5.2 Location Legend

- **Config:** JSON/YAML in repo (`StreamingAssets/CCAS/phase2_config.json`)
- **File:** Hardcoded or asset (`Assets/Scripts/CCAS/EmotionalStateManager.cs`)
- **Env:** Environment variable
- **DB:** Database / remote config

### 5.3 Change Impact Summary

- **Capabilities:**
  - `P_max`, `N_max`, pack bias exponents, and curve exponents dictate the core emotional feel and progression speed
  - Quality, streak, and economy thresholds determine when each emotional bucket activates (the trigger logic)
  - Decay and recovery values shape the session arc, building a persistent narrative or making each pull feel isolated
  - Duplicate XP controls pure progression and duplicate fairness, unlinked to the visual emotional bars
- **Trade-offs:**
  - Increasing positive emotion levers (like `good_pull_reduces_negative` or `P_max`) makes the game feel generous but trivializes difficult pulls, leading to flat long-term satisfaction
  - Increasing negative emotion levers (like `hot_streak_threshold` or `quality_bad_threshold`) makes the game feel riskier and more engaging, but risks player churn if punishment dominates
  - Modifying the decay lengths trades off between momentary spikes (fast decay) vs a lingering shadow or halo effect over a session (slow decay)

---

## 6. Documentation Status

| Section | Status |
|---------|--------|
| Code ownership | ✅ Done |
| Schemas/contracts | ✅ Done |
| Workflows | ✅ Done |
| Integration (emit) | ✅ Done |
| Integration (consume) | ✅ Done |
| Tunable values | ✅ Done |

**Handoff Notes:**

- **Immediate next step — add Elite and Supreme pack types.** Source of truth is `Assets/Resources/CCAS/Phase2/Phase2_CardPacksandTypes.csv` (Vrushali's sheet). Add `elite_pack` and `supreme_pack` to `phase2_config.json` under `pack_types` with the same shape as existing packs. Drop rates from the sheet: Elite (Rare 70%, Epic 15%, Legendary 15%), Supreme (Epic 50%, Legendary 50%). All 5 packs use 3 cards (`guaranteed_cards: 3`). No code changes needed — BoosterMarketAuto is config-driven. Costs and `score_range` for Elite and Supreme still need to be decided.
- **Purchase flow not wired.** `PlayerWallet.SpendForPack` exists and Economy contracts are defined, but the Economy integration is not yet connected. Packs currently open immediately on click. Wiring this is a known next step.
- **Duplicate + emotion is a future design space.** Do not implement until the full system (upgrades, progression, teams) is clearer. Research the cases first (duplicate that upgrades a card vs one that doesn't, chasing a specific card, etc.) before touching EmotionalStateManager. See `Assets/Resources/CCAS/CCAS_Future_Plan.md` for full mindset and approach.
- **Contract repo/DocMost paths are TBD.** Economy and Progression contracts are documented in the dependency table PDFs but formal repo/DocMost paths have not been assigned. Align with integration team once standardized doc is posted.
- **Reference docs for the next person:**
  - Script quick lookup: `Assets/Resources/CCAS/reference/CCAS_Script_Reference_Overview.md`
  - Full script detail: `Assets/Resources/CCAS/reference/CCAS_Scripts_Detailed.md`
  - Workflow flowcharts: `Assets/Resources/CCAS/reference/CCAS_Workflow_Flowchart.md`
  - Every file and folder: `Assets/Resources/CCAS/reference/CCAS_File_Map.md`
  - Future plan: `Assets/Resources/CCAS/CCAS_Future_Plan.md`
  - Phase 2 emotion spec: `Assets/Resources/CCAS/Phase2/Phase2_Emotional_System_Specification.md`
  - Phase 1 duplicate conversion: `Assets/Resources/CCAS/Phase1/Phase 1 – Part 4: Duplicate Conversion System.pdf`


