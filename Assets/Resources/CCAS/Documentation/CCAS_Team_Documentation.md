# Team Documentation — CCAS

---

## Owner Overview

| Field | Value |
|-------|-------|
| **Owner** | Bhavik Upadhyay, Vrushali Ghotavadekar |
| **Team** | CCAS |
| **Last Updated** | 2026-03-02 |

---

## 5. Tunable Values & Rationale

> Config file: `StreamingAssets/CCAS/phase2_config.json`
> Schema Version: `ccas.p2_emotion_families.v1`

### 5.1 Inventory (CCAS area)

#### 5.1.1 Rarity Values
Defined in `phase2_config.json → rarity_values`. Used to compute the raw score from a pack pull. Each card's rarity maps to a numeric point value.

| Variable | Default | Location | Rationale | Impact of Change |
|----------|---------|----------|-----------|------------------|
| `common` rarity value | 1 | `phase2_config.json` | Baseline card value | **Higher:** Inflates all pack scores. |
| `uncommon` rarity value | 2 | `phase2_config.json` | 2x common | **Higher:** Uncommons carry more weight. |
| `rare` rarity value | 3 | `phase2_config.json` | 3x common | **Higher:** Rares drive scores up faster. |
| `epic` rarity value | 4 | `phase2_config.json` | 4x common | **Higher:** Epics dominate pack quality. |
| `legendary` rarity value | 5 | `phase2_config.json` | 5x common | **Higher:** Legendaries guarantee max quality. |

#### 5.1.2 Pack Types
Defined in `phase2_config.json → pack_types`. Each pack is a keyed entry with sub-fields that control cost, yield, and emotional scoring boundaries. Drop rates and score ranges establish the baseline expected quality for each pack.

| Variable | Default | Location | Rationale | Impact of Change |
|----------|---------|----------|-----------|------------------|
| Bronze Score Range | [3, 7] | `phase2_config.json` | Max/Min score bounds for Bronze | Shifts quality calculation baseline |
| Silver Score Range | [6, 12] | `phase2_config.json` | Max/Min score bounds for Silver | Shifts quality calculation baseline |
| Gold Score Range | [9, 13] | `phase2_config.json` | Max/Min score bounds for Gold | Shifts quality calculation baseline |

#### 5.1.3 Emotion Parameters
Defined in `phase2_config.json → phase_2_configuration.emotion_parameters`. Controls the magnitude of emotional deltas each pull can produce.

| Variable | Default | Location | Rationale | Impact of Change |
|----------|---------|----------|-----------|------------------|
| `P_max` | 3.0 | `phase2_config.json` | Ceiling for positive emotion per pull | **Higher:** Bar fills faster, may trivialize difficult pulls |
| `N_max` | 2.0 | `phase2_config.json` | Ceiling for negative emotion per pull; asymmetrical to favor recovery | **Higher:** Bad pulls feel much more punishing |
| `P_cap` / `N_cap` | 100.0 | `phase2_config.json` | Hard ceiling limits to keep UI bars meaningful | **Lower:** Emotions saturate quickly, reducing granularity |
| Positive curve exponent | 0.7 | `EmotionalStateManager.cs` | `pow(quality01, 0.7)` | Flattens or steepens positive acquisition |
| Negative curve exponent | 1.2 | `EmotionalStateManager.cs` | `pow(1−quality01, 1.2)` | Flattens or steepens negative acquisition |
| Rare boost multiplier | 1.15 | `EmotionalStateManager.cs` | `dP` boost when Rare/Epic/Leg present | Modifies impact of pulling high rarity cards |

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
| `high_cost_threshold_coins`| 1500 | `phase2_config.json` | Min cost to trigger economy-regret routing | **Lower:** Cheap packs can trigger Regret |

#### 5.1.6 Phase 2 Decay Parameters
Defined in `phase2_config.json → phase_2_configuration.decay`. Applied **before** new deltas each pull. Lower value = faster fade.

| Variable | Default | Location | Rationale | Impact of Change |
|----------|---------|----------|-----------|------------------|
| Pos/Neg `rarity_pack` decay| 0.985 | `phase2_config.json` | Thrill/Disappointment linger (~1.5% fade) | **Lower:** Fast fade; emotion is gone in a pull or two |
| Pos/Neg `streak` decay | 0.920 | `phase2_config.json` | Relief/Letdown are momentary (~8% fade) | **Higher:** Streak emotions persist past context |
| Pos/Neg `economy` decay | 0.960 | `phase2_config.json` | Worth/Regret fade moderately (~4% fade) | **Higher:** Lasting session shadow |

#### 5.1.7 Phase 2 Recovery Parameters
Defined in `phase2_config.json → phase_2_configuration.recovery`. Controls cross-family dampening — good pulls reduce the negative bar and bad pulls reduce the positive bar.

| Variable | Default | Location | Rationale | Impact of Change |
|----------|---------|----------|-----------|------------------|
| `good_pull_reduces_negative`| 0.50 | `phase2_config.json` | Good pulls push back on negative bar (50%) | **Higher:** Very forgiving; negative vanishes quickly |
| `bad_pull_reduces_positive` | 0.50 | `phase2_config.json` | Bad pulls dent positive bar (50%) | **Lower:** Positive bar is sticky/impervious |
| `recovery.enabled` | true | `phase2_config.json` | Master toggle for cross-family dampening | **Disabled:** Bars independent; breaks tug-of-war |

#### 5.1.8 Duplicate XP
Defined in `phase2_config.json → duplicate_xp`. Controls how many XP points a player receives when opening a duplicate card.

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
  - `P_max`, `N_max`, pack bias exponents, and curve exponents dictate the core emotional feel and progression speed.
  - Quality, streak, and economy thresholds determine *when* each emotional bucket activates (the trigger logic).
  - Decay and recovery values shape the session arc, building a persistent narrative or making each pull feel isolated.
  - Duplicate XP controls pure progression and duplicate fairness, unlinked to the visual emotional bars.
- **Trade-offs:** 
  - Increasing positive emotion levers (like `good_pull_reduces_negative` or `P_max`) makes the game feel generous but trivializes difficult pulls, leading to flat long-term satisfaction.
  - Increasing negative emotion levers (like `hot_streak_threshold` or `quality_bad_threshold`) makes the game feel riskier and more engaging, but risks player churn if punishment dominates.
  - Modifying the decay lengths trade off between momentary spikes (fast decay) vs a lingering "shadow" or "halo" effect over a session (slow decay).
