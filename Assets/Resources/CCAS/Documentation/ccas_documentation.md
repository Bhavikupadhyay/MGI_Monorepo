# CCAS – Tunable Variables Reference

**System:** Card Collection Acquisition System (CCAS)  
**Config File:** `StreamingAssets/CCAS/phase2_config.json`  
**Schema Version:** `ccas.p2_emotion_families.v1`  
**Last Updated:** 2026-02-26

---

## Table of Contents

1. [Rarity Values](#1-rarity-values)
2. [Pack Types](#2-pack-types)
3. [Phase 2 Emotion Parameters](#3-phase-2-emotion-parameters)
4. [Phase 2 Family Weights](#4-phase-2-family-weights)
5. [Phase 2 Routing Thresholds](#5-phase-2-routing-thresholds)
6. [Phase 2 Decay](#6-phase-2-decay)
7. [Phase 2 Recovery](#7-phase-2-recovery)
8. [Legacy Phase 1 Emotion Dynamics](#8-legacy-phase-1-emotion-dynamics)
9. [Duplicate XP](#9-duplicate-xp)
10. [Inspector-Exposed Fallbacks (EmotionalStateManager)](#10-inspector-exposed-fallbacks-emotionalstatemanager)
11. [Hardcoded Constants](#11-hardcoded-constants)
12. [Quick-Tuning Guide](#12-quick-tuning-guide)

---

## 1. Rarity Values

Defined in `phase2_config.json → rarity_values`. Used to compute the raw score from a pack pull. Each card's rarity maps to a numeric point value.

| Rarity Key  | `numeric_value`                  | Default |
| ----------- | -------------------------------- | ------- |
| `common`    | numeric value added to raw score | **1**   |
| `uncommon`  | numeric value added to raw score | **2**   |
| `rare`      | numeric value added to raw score | **3**   |
| `epic`      | numeric value added to raw score | **4**   |
| `legendary` | numeric value added to raw score | **5**   |

> **Source:** `DropConfigModels.cs → RarityValue`, `EmotionalStateManager.cs → GetRarityNumericValue()`

---

## 2. Pack Types

Defined in `phase2_config.json → pack_types`. Each pack is a keyed entry with sub-fields that control cost, yield, and emotional scoring boundaries.

### 2.1 Bronze Pack (`bronze_pack`)

| Variable                | Default        | Description                                      |
| ----------------------- | -------------- | ------------------------------------------------ |
| `cost`                  | **1000** coins | Price of the pack; used in economy value scoring |
| `guaranteed_cards`      | **3**          | Number of cards per pack open                    |
| `drop_rates.common`     | **0.75**       | Weighted pull probability for Common             |
| `drop_rates.uncommon`   | **0.15**       | Weighted pull probability for Uncommon           |
| `drop_rates.rare`       | **0.10**       | Weighted pull probability for Rare               |
| `score_range.min_score` | **3**          | Raw score at which quality01 = 0.0               |
| `score_range.max_score` | **7**          | Raw score at which quality01 = 1.0               |

### 2.2 Silver Pack (`silver_pack`)

| Variable                | Default        | Description                                                           |
| ----------------------- | -------------- | --------------------------------------------------------------------- |
| `cost`                  | **1500** coins | Price; triggers economy routing if cost ≥ `high_cost_threshold_coins` |
| `guaranteed_cards`      | **3**          | Number of cards per pack open                                         |
| `drop_rates.uncommon`   | **0.65**       | Weighted pull probability for Uncommon                                |
| `drop_rates.rare`       | **0.20**       | Weighted pull probability for Rare                                    |
| `drop_rates.epic`       | **0.10**       | Weighted pull probability for Epic                                    |
| `drop_rates.legendary`  | **0.05**       | Weighted pull probability for Legendary                               |
| `score_range.min_score` | **6**          | Raw score at which quality01 = 0.0                                    |
| `score_range.max_score` | **12**         | Raw score at which quality01 = 1.0                                    |

### 2.3 Gold Pack (`gold_pack`)

| Variable                | Default        | Description                                  |
| ----------------------- | -------------- | -------------------------------------------- |
| `cost`                  | **2000** coins | Price; always triggers economy routing check |
| `guaranteed_cards`      | **3**          | Number of cards per pack open                |
| `drop_rates.rare`       | **0.60**       | Weighted pull probability for Rare           |
| `drop_rates.epic`       | **0.30**       | Weighted pull probability for Epic           |
| `drop_rates.legendary`  | **0.10**       | Weighted pull probability for Legendary      |
| `score_range.min_score` | **9**          | Raw score at which quality01 = 0.0           |
| `score_range.max_score` | **13**         | Raw score at which quality01 = 1.0           |

### 2.4 Pack-Type Quality Bias (Hardcoded)

Applied after normalization to shape player perception of each pack tier.

| Pack   | Power exponent | Effect                                           |
| ------ | -------------- | ------------------------------------------------ |
| Bronze | **0.8**        | Optimistic — inflates perceived quality slightly |
| Silver | **1.0**        | Neutral — no bias                                |
| Gold   | **1.2**        | Stricter — requires a better pull to feel "good" |

> **Source:** `EmotionalStateManager.cs → AdjustQualityForPack()`

### 2.5 Pack-Type Rarity Expectation Thresholds (Hardcoded)

Determines when a pull's best rarity is considered "special" (drives Thrill routing).

| Pack    | Min `maxRarityNumeric` to be "special" |
| ------- | -------------------------------------- |
| Bronze  | **3** (Rare+)                          |
| Silver  | **4** (Epic+)                          |
| Gold    | **5** (Legendary only)                 |
| Default | **4** (Epic+)                          |

> **Source:** `EmotionalStateManager.cs → IsMaxRaritySpecialForPack()`

---

## 3. Phase 2 Emotion Parameters

Defined in `phase2_config.json → phase_2_configuration.emotion_parameters`. Controls the magnitude of emotional deltas each pull can produce.

| Variable | Default   | Description                                             |
| -------- | --------- | ------------------------------------------------------- |
| `P_max`  | **3.0**   | Maximum raw positive delta per pull (before rare boost) |
| `N_max`  | **2.0**   | Maximum raw negative delta per pull                     |
| `P_cap`  | **100.0** | Hard ceiling for the positive family level              |
| `N_cap`  | **100.0** | Hard ceiling for the negative family level              |

**Delta curves (hardcoded exponents):**

| Curve    | Formula                           | Exponent | Effect                                       |
| -------- | --------------------------------- | -------- | -------------------------------------------- |
| Positive | `pow(quality01, 0.7) × P_max`     | **0.7**  | Easier to gain at lower quality; concave     |
| Negative | `pow(1 − quality01, 1.2) × N_max` | **1.2**  | Harder to gain at borderline quality; convex |

**Rare card boost (hardcoded):**

| Condition                              | Multiplier | Applied to            |
| -------------------------------------- | ---------- | --------------------- |
| Pull contains Rare, Epic, or Legendary | **×1.15**  | `dP` (positive delta) |

> **Source:** `EmotionalStateManager.cs → GetMaxesPhase2()`, `ApplyPackOutcome()`

---

## 4. Phase 2 Family Weights

Defined in `phase2_config.json → phase_2_configuration.families`. The three bucket values are combined using these normalized weights to produce the final `positive` and `negative` bar values.

| Family   | Bucket        | Default Weight |
| -------- | ------------- | -------------- |
| Positive | `rarity_pack` | **0.50**       |
| Positive | `streak`      | **0.30**       |
| Positive | `economy`     | **0.20**       |
| Negative | `rarity_pack` | **0.50**       |
| Negative | `streak`      | **0.30**       |
| Negative | `economy`     | **0.20**       |

Formula: `family_level = (bucket_rp × w_rp + bucket_st × w_st + bucket_ec × w_ec) / (w_rp + w_st + w_ec)`

> **Source:** `EmotionalStateManager.cs → RecomputeFamilyLevels()`, `DropConfigModels.cs → Phase2Family`

---

## 5. Phase 2 Routing Thresholds

Defined in `phase2_config.json → phase_2_configuration.routing`. These thresholds decide which emotional buckets participate per pull and how strongly.

### 5.1 Quality Thresholds

| Variable                 | Default  | Description                                                                                      |
| ------------------------ | -------- | ------------------------------------------------------------------------------------------------ |
| `quality_good_threshold` | **0.62** | quality01 ≥ this activates positive rarity-pack routing (weight 0.6) and positive streak routing |
| `quality_peak_threshold` | **0.85** | quality01 ≥ this activates maximum positive rarity-pack routing (weight 1.0)                     |
| `quality_bad_threshold`  | **0.38** | quality01 ≤ this activates negative rarity-pack routing (weight 0.7)                             |

### 5.2 Streak / Rolling Window

| Variable                | Default  | Description                                                                      |
| ----------------------- | -------- | -------------------------------------------------------------------------------- |
| `streak_window`         | **5**    | Number of past pulls kept in the rolling quality window                          |
| `cold_streak_threshold` | **0.40** | Rolling avg quality below this = "cold mood" (amplifies positive streak routing) |
| `hot_streak_threshold`  | **0.60** | Rolling avg quality above this = "hot mood" (amplifies negative streak routing)  |

### 5.3 Economy / Value Score

| Variable                    | Default    | Description                                                                                                |
| --------------------------- | ---------- | ---------------------------------------------------------------------------------------------------------- |
| `value_score_scale`         | **1000.0** | Multiplier: `valueScore = (rawScore / cost) × scale`                                                       |
| `value_good_threshold`      | **2.20**   | valueScore ≥ this activates positive economy routing (weight 0.6)                                          |
| `value_bad_threshold`       | **1.80**   | valueScore ≤ this (and cost ≥ `high_cost_threshold_coins`) activates negative economy routing (weight 1.0) |
| `high_cost_threshold_coins` | **1500**   | Minimum pack cost for economy-regret routing to trigger                                                    |

### 5.4 Positive Routing Weight Summary

| Bucket        | Condition                                                            | Raw Weight |
| ------------- | -------------------------------------------------------------------- | ---------- |
| `rarity_pack` | quality01 ≥ `quality_peak_threshold` OR rarity is "special for pack" | **1.0**    |
| `rarity_pack` | quality01 ≥ `quality_good_threshold` (non-peak)                      | **0.6**    |
| `rarity_pack` | quality01 < `quality_good_threshold`                                 | 0          |
| `streak`      | cold mood AND quality01 ≥ `quality_good_threshold`                   | **0.9**    |
| `streak`      | otherwise                                                            | 0          |
| `economy`     | valueScore ≥ `value_good_threshold`                                  | **0.6**    |
| `economy`     | otherwise                                                            | 0          |

### 5.5 Negative Routing Weight Summary

| Bucket        | Condition                                                                     | Raw Weight |
| ------------- | ----------------------------------------------------------------------------- | ---------- |
| `rarity_pack` | quality01 ≤ `quality_bad_threshold`                                           | **0.7**    |
| `rarity_pack` | otherwise                                                                     | 0          |
| `streak`      | hot mood AND quality01 ≤ 0.5                                                  | **0.9**    |
| `streak`      | otherwise                                                                     | 0          |
| `economy`     | cost ≥ `high_cost_threshold_coins` AND 0 < valueScore ≤ `value_bad_threshold` | **1.0**    |
| `economy`     | otherwise                                                                     | 0          |

> Weights are normalized to sum to 1 before being applied.  
> **Source:** `EmotionalStateManager.cs → ApplyPositiveRouting()`, `ApplyNegativeRouting()`

---

## 6. Phase 2 Decay

Defined in `phase2_config.json → phase_2_configuration.decay`. Applied **before** new deltas each pull. Lower value = faster fade.

| Family   | Bucket        | Default Multiplier | Decay per pull |
| -------- | ------------- | ------------------ | -------------- |
| Positive | `rarity_pack` | **0.985**          | ~1.5% per pull |
| Positive | `streak`      | **0.920**          | ~8% per pull   |
| Positive | `economy`     | **0.960**          | ~4% per pull   |
| Negative | `rarity_pack` | **0.985**          | ~1.5% per pull |
| Negative | `streak`      | **0.920**          | ~8% per pull   |
| Negative | `economy`     | **0.960**          | ~4% per pull   |

All bucket values clamped to `[0, 100]` after decay.

> **Source:** `EmotionalStateManager.cs → ApplyPhase2Decay()`

---

## 7. Phase 2 Recovery

Defined in `phase2_config.json → phase_2_configuration.recovery`. Controls cross-family dampening — good pulls reduce the negative bar and bad pulls reduce the positive bar.

| Variable                     | Default  | Description                                                                                            |
| ---------------------------- | -------- | ------------------------------------------------------------------------------------------------------ |
| `enabled`                    | **true** | Master toggle; if false, recovery is entirely skipped                                                  |
| `good_pull_reduces_negative` | **0.50** | Fraction of `posApplied` subtracted from the negative family when quality01 ≥ `quality_good_threshold` |
| `bad_pull_reduces_positive`  | **0.50** | Fraction of `negApplied` subtracted from the positive family when quality01 ≤ `quality_bad_threshold`  |

Reduction is distributed across all three buckets of the affected family proportionally to their current values.

> **Source:** `EmotionalStateManager.cs → ApplyPhase2Recovery()`

---

## 8. Legacy Phase 1 Emotion Dynamics

Defined in `phase2_config.json → emotion_dynamics`. These were active in Phase 1 and are retained in the config for backwards compatibility. The Phase 2 `EmotionalStateManager` does **not** use these fields directly in its main pipeline, but they document the Phase 1 tuning assumptions.

### 8.1 Emotion Parameters (Phase 1)

| Variable | Default   | Description                     |
| -------- | --------- | ------------------------------- |
| `S_max`  | **3.0**   | Max satisfaction delta per pull |
| `F_max`  | **2.0**   | Max frustration delta per pull  |
| `S_cap`  | **100.0** | Satisfaction ceiling            |
| `F_cap`  | **100.0** | Frustration ceiling             |

### 8.2 Quality Reset

| Variable         | Default  | Description                                                  |
| ---------------- | -------- | ------------------------------------------------------------ |
| `enabled`        | **true** | Toggle for the quality-driven reduction step                 |
| `R_S`            | **1.5**  | Legacy: no longer used in bad-pull path                      |
| `R_F`            | **1.5**  | Frustration reduction amount for good pulls                  |
| `good_threshold` | **0.5**  | quality01 above this triggers frustration reduction          |
| `bad_threshold`  | **0.3**  | quality01 below this receives reduced frustration punishment |

### 8.3 Neutral Band

| Variable   | Default  | Description                                                            |
| ---------- | -------- | ---------------------------------------------------------------------- |
| `enabled`  | **true** | Toggle for neutral-band recovery step                                  |
| `min`      | **0.38** | Lower quality01 bound of the "neutral" zone                            |
| `max`      | **0.62** | Upper quality01 bound of the "neutral" zone                            |
| `recovery` | **0.35** | Dampening factor applied to both deltas in neutral zone (Phase 1 only) |

When quality01 ∈ [min, max], both satisfaction and frustration deltas are multiplied by **0.85** in the Phase 1 implementation.

### 8.4 Oppositional Dampening

| Variable  | Default  | Effective value in code | Description                                                                        |
| --------- | -------- | ----------------------- | ---------------------------------------------------------------------------------- |
| `enabled` | **true** | —                       | Toggle for oppositional coupling                                                   |
| `k`       | **0.25** | **0.075** (0.25 × 0.3)  | Cross-dampening coefficient; hard-reduced by 70% in code to prevent feedback loops |

### 8.5 Streak (Phase 1)

| Variable    | Default  | Effective value in code  | Description                                     |
| ----------- | -------- | ------------------------ | ----------------------------------------------- | ---------- | ------------------------ |
| `enabled`   | **true** | —                        | Toggle for streak multiplier                    |
| `window`    | **5**    | —                        | Number of past pulls in rolling average         |
| `alpha`     | **1.0**  | **0.5** (halved in code) | Satisfaction amplification from hot/cold streak |
| `beta`      | **1.0**  | **0.5** (halved in code) | Frustration amplification from hot/cold streak  |
| `threshold` | **0.1**  | —                        | Minimum `                                       | qAvg − 0.5 | ` for streak to activate |

> **Source:** `phase2_config.json → emotion_dynamics`, `Emotion Formula Simplification_part2_UPDATED.md`

---

## 9. Duplicate XP

Defined in `phase2_config.json → duplicate_xp`. Controls how many XP points a player receives when opening a duplicate card (Phase 1 – Part 4 system).

| Variable                 | Default | Rarity    |
| ------------------------ | ------- | --------- |
| `common_duplicate_xp`    | **5**   | Common    |
| `uncommon_duplicate_xp`  | **10**  | Uncommon  |
| `rare_duplicate_xp`      | **25**  | Rare      |
| `epic_duplicate_xp`      | **50**  | Epic      |
| `legendary_duplicate_xp` | **100** | Legendary |

> **Source:** `DropConfigModels.cs → DuplicateXP`

---

## 10. Inspector-Exposed Fallbacks (EmotionalStateManager)

These Unity Inspector fields on `EmotionalStateManager` are **only used if no JSON config is loaded** (i.e., `phase2_config.json` is missing or `phase_2_configuration.emotion_parameters` is absent).

| Inspector Field  | Default  | Description                                            |
| ---------------- | -------- | ------------------------------------------------------ |
| `P_max_Fallback` | **3.0**  | Fallback max positive delta                            |
| `N_max_Fallback` | **2.0**  | Fallback max negative delta                            |
| `verbose`        | **true** | Enables detailed per-pull logging to the Unity console |

The two family-level bars (`negative`, `positive`) and all six bucket fields (`pos_rarity_pack`, `pos_streak`, `pos_economy`, `neg_rarity_pack`, `neg_streak`, `neg_economy`) are also visible in the Inspector for real-time live monitoring during play mode.

> **Source:** `EmotionalStateManager.cs` lines 16–37

---

## 11. Hardcoded Constants

These values are baked into source code and are **not** overridable by JSON config (as of current implementation). Changing them requires modifying the C# script.

| Location                   | Constant                                 | Value               | Description                                              |
| -------------------------- | ---------------------------------------- | ------------------- | -------------------------------------------------------- |
| `EmotionalStateManager.cs` | Positive curve exponent                  | **0.7**             | `pow(quality01, 0.7)`                                    |
| `EmotionalStateManager.cs` | Negative curve exponent                  | **1.2**             | `pow(1−quality01, 1.2)`                                  |
| `EmotionalStateManager.cs` | Rare boost multiplier                    | **1.15**            | Applied to `dP` when Rare/Epic/Legendary present         |
| `EmotionalStateManager.cs` | Positive rarity weight (peak)            | **1.0**             | Full weight when quality01 ≥ `quality_peak_threshold`    |
| `EmotionalStateManager.cs` | Positive rarity weight (good)            | **0.6**             | Partial weight when quality01 ≥ `quality_good_threshold` |
| `EmotionalStateManager.cs` | Positive streak weight                   | **0.9**             | Applied when cold mood + quality is good                 |
| `EmotionalStateManager.cs` | Positive economy weight                  | **0.6**             | Applied when good value score                            |
| `EmotionalStateManager.cs` | Negative rarity weight                   | **0.7**             | Applied when quality ≤ `quality_bad_threshold`           |
| `EmotionalStateManager.cs` | Negative streak weight                   | **0.9**             | Applied when hot mood + quality ≤ 0.5                    |
| `EmotionalStateManager.cs` | Negative economy weight                  | **1.0**             | Applied when high cost + bad value                       |
| `EmotionalStateManager.cs` | Hot mood negative streak quality ceiling | **0.5**             | quality01 must be ≤ 0.5 for neg streak to apply          |
| `EmotionalStateManager.cs` | Oppositional reduction factor            | **0.3**             | The config `k` is multiplied by this (Phase 1 only)      |
| `EmotionalStateManager.cs` | Phase 1 hot streak sat multiplier        | **0.5**             | Halved from config `alpha`                               |
| `EmotionalStateManager.cs` | Phase 1 hot streak frust multiplier      | **0.5**             | Halved from config `beta`                                |
| `EmotionalStateManager.cs` | Phase 1 cold streak sat factor           | **0.5**             | Dampens cold-streak satisfaction reduction               |
| `EmotionalStateManager.cs` | Phase 1 cold streak frust factor         | **0.7**             | Dampens cold-streak frustration gain                     |
| `EmotionalStateManager.cs` | Bucket value clamp                       | **[0, 100]**        | All bucket values are clamped to this range              |
| `EmotionalStateManager.cs` | Bronze pack bias exponent                | **0.8**             | Quality perception adjustment                            |
| `EmotionalStateManager.cs` | Silver pack bias exponent                | **1.0**             | Neutral                                                  |
| `EmotionalStateManager.cs` | Gold pack bias exponent                  | **1.2**             | Quality perception adjustment                            |
| `EmotionalStateManager.cs` | Bronze rarity special threshold          | **≥ 3**             | Rare or better considered "special" for Bronze           |
| `EmotionalStateManager.cs` | Silver rarity special threshold          | **≥ 4**             | Epic or better considered "special" for Silver           |
| `EmotionalStateManager.cs` | Gold rarity special threshold            | **≥ 5**             | Legendary only considered "special" for Gold             |
| Config fallback (all)      | Bronze score range                       | **[3, 7]**          | Used if pack key not in `pack_types`                     |
| Config fallback (all)      | Silver score range                       | **[6, 12]**         | Used if pack key not in `pack_types`                     |
| Config fallback (all)      | Gold score range                         | **[9, 13]**         | Used if pack key not in `pack_types`                     |
| Config fallback (all)      | Default positive family weights          | **0.5 / 0.3 / 0.2** | rarity_pack / streak / economy                           |

---

## 12. Quick-Tuning Guide

Use this section as a rapid reference when adjusting feel without a full re-read of spec docs.

### Positive Emotions Feel Too Strong

- Lower `P_max` (e.g. 3.0 → 2.0)
- Raise `quality_good_threshold` (e.g. 0.62 → 0.70)
- Raise `quality_peak_threshold` (e.g. 0.85 → 0.90)
- Reduce `good_pull_reduces_negative` (e.g. 0.50 → 0.25)
- Increase `decay.positive.rarity_pack` closer to 1.0 (slower decay = faster fade requires _lower_ value; increase for longer persistence)

### Positive Emotions Feel Too Weak / Don't Build

- Raise `P_max` (e.g. 3.0 → 4.0)
- Lower `quality_good_threshold` (e.g. 0.62 → 0.55)
- Raise `good_pull_reduces_negative` (e.g. 0.50 → 0.70)
- Lower `cold_streak_threshold` so cold mood kicks in later (e.g. 0.40 → 0.35)

### Negative Emotions Feel Too Punishing

- Lower `N_max` (e.g. 2.0 → 1.5)
- Lower `quality_bad_threshold` (e.g. 0.38 → 0.30)
- Raise `hot_streak_threshold` so hot mood is harder to trigger (e.g. 0.60 → 0.70)
- Lower `bad_pull_reduces_positive` (e.g. 0.50 → 0.25)
- Raise `high_cost_threshold_coins` so economy regret trigger is harder to hit

### Negative Emotions Recover Too Fast

- Lower `decay.negative.rarity_pack` (e.g. 0.985 → 0.995 to slow decay)
- Lower `good_pull_reduces_negative` (e.g. 0.50 → 0.20)

### Streaks Feel Too Impactful

- Raise `cold_streak_threshold` and `hot_streak_threshold` closer together (narrower neutral zone)
- Reduce `streak_window` (e.g. 5 → 3) for shorter streak memory
- Note: Phase 2 streak routing has hardcoded weight caps (0.9); adjust the negative streak quality ceiling (0.5) in code if needed

### Economy Routing Fires Too Often / Too Rarely

- Adjust `value_good_threshold` and `value_bad_threshold` (currently 2.2 / 1.8)
- Adjust `value_score_scale` (currently 1000)
- Adjust `high_cost_threshold_coins` (currently 1500)

### Rare Cards Feel Underwhelming

- Increase the rare boost multiplier (currently **1.15** in code, `EmotionalStateManager.cs`)
- Lower the Silver/Gold rarity specialness thresholds so more card types trigger Thrill routing

### Drop Rates Adjustment

- Edit `drop_rates` fields per pack in `phase2_config.json`
- Rates are weighted (not required to sum to 1.0); unspecified rarities default to 0

---

_For full formula derivations, see:_

- `Phase1/Emotion Formula Simplification_part2_UPDATED.md` — Phase 1 derivation with worked examples
- `Phase2/Phase2_Emotional_System_Specification.md` — Phase 2 architecture and pipeline spec
- `reference/CCAS_Scripts_Detailed.md` — Per-script behavior reference
