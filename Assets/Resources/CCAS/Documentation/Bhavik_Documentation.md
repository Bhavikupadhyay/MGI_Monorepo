# Team Member Documentation — Bhavik Upadhyay

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

### 5.1 Rarity Values

Defined in `phase2_config.json → rarity_values`. Used to compute the raw score from a pack pull. Each card's rarity maps to a numeric point value.

| Rarity Key  | `numeric_value`                  | Default | Rationale | Impact of Change |
| ----------- | -------------------------------- | ------- | --------- | ---------------- |
| `common`    | numeric value added to raw score | **1**   | Baseline card value | **Higher:** Inflates all pack scores. |
| `uncommon`  | numeric value added to raw score | **2**   | 2x common | **Higher:** Uncommons carry more weight. |
| `rare`      | numeric value added to raw score | **3**   | 3x common | **Higher:** Rares drive scores up faster. |
| `epic`      | numeric value added to raw score | **4**   | 4x common | **Higher:** Epics dominate pack quality. |
| `legendary` | numeric value added to raw score | **5**   | 5x common | **Higher:** Legendaries guarantee max quality. |

### 5.2 Pack Types

Defined in `phase2_config.json → pack_types`. Each pack is a keyed entry with sub-fields that control cost, yield, and emotional scoring boundaries.

#### Bronze Pack (`bronze_pack`)

| Variable                | Default        | Description                                      |
| ----------------------- | -------------- | ------------------------------------------------ |
| `cost`                  | **1000** coins | Price of the pack; used in economy value scoring |
| `guaranteed_cards`      | **3**          | Number of cards per pack open                    |
| `drop_rates.common`     | **0.75**       | Weighted pull probability for Common             |
| `drop_rates.uncommon`   | **0.15**       | Weighted pull probability for Uncommon           |
| `drop_rates.rare`       | **0.10**       | Weighted pull probability for Rare               |
| `score_range.min_score` | **3**          | Raw score at which quality01 = 0.0               |
| `score_range.max_score` | **7**          | Raw score at which quality01 = 1.0               |

#### Silver Pack (`silver_pack`)

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

#### Gold Pack (`gold_pack`)

| Variable                | Default        | Description                                  |
| ----------------------- | -------------- | -------------------------------------------- |
| `cost`                  | **2000** coins | Price; always triggers economy routing check |
| `guaranteed_cards`      | **3**          | Number of cards per pack open                |
| `drop_rates.rare`       | **0.60**       | Weighted pull probability for Rare           |
| `drop_rates.epic`       | **0.30**       | Weighted pull probability for Epic           |
| `drop_rates.legendary`  | **0.10**       | Weighted pull probability for Legendary      |
| `score_range.min_score` | **9**          | Raw score at which quality01 = 0.0           |
| `score_range.max_score` | **13**         | Raw score at which quality01 = 1.0           |

### 5.3 Emotion Parameters

Defined in `phase2_config.json → phase_2_configuration.emotion_parameters`. Controls the magnitude of emotional deltas each pull can produce.

| Variable | Default | Location | Rationale | Impact of Change |
|----------|---------|----------|-----------|------------------|
| `P_max` | `3.0` | `...emotion_parameters` | Sets the ceiling for how much positive emotion a single pull can generate. Value of 3.0 allows meaningful accumulation over a session without the bar maxing out too quickly. | **Higher:** Positive bar fills faster; sessions feel very rewarding but may trivialize difficult pulls. **Lower:** Satisfaction feels flat; good pulls barely register. |
| `N_max` | `2.0` | `...emotion_parameters` | Kept intentionally lower than `P_max` to create an asymmetric, recovery-friendly system — bad pulls sting less than good pulls reward. | **Higher:** Bad pulls feel much more punishing; frustration dominates sessions. **Lower:** Negative emotions barely build, reducing perceived risk of spending coins. |
| `P_cap` / `N_cap` | `100.0` each | `...emotion_parameters` | Hard ceiling prevents runaway emotion states and keeps UI bars meaningful across the 0–100 display range. | **Higher:** Allows more extreme states; UI bars rarely reach max, reducing visual feedback impact. **Lower:** Emotions saturate quickly, reducing per-pull granularity. |

### 5.4 Phase 2 Family Weights

Defined in `phase2_config.json → phase_2_configuration.families`. The three bucket values are combined using these normalized weights to produce the final `positive` and `negative` bar values.

Formula: `family_level = (bucket_rp × w_rp + bucket_st × w_st + bucket_ec × w_ec) / (w_rp + w_st + w_ec)`

| Family   | Bucket        | Default Weight | Rationale | Impact of Change |
| -------- | ------------- | -------------- | --------- | ---------------- |
| Positive | `rarity_pack` | **0.50**       | Core driver of excitement. | **Higher:** Pull quality dominates mood. |
| Positive | `streak`      | **0.30**       | Secondary driver based on luck streaks. | **Higher:** Streaks feel more impactful. |
| Positive | `economy`     | **0.20**       | Tertiary driver based on cost vs value. | **Higher:** Economy choices matter more. |
| Negative | `rarity_pack` | **0.50**       | Core driver of disappointment. | **Higher:** Bad pulls hurt more. |
| Negative | `streak`      | **0.30**       | Secondary driver for bad luck streaks. | **Higher:** Cold streaks feel worse. |
| Negative | `economy`     | **0.20**       | Tertiary driver for bad investments. | **Higher:** Expensive bad pulls sting more. |

### 5.5 Routing Thresholds

Defined in `phase2_config.json → phase_2_configuration.routing`. These thresholds decide which emotional buckets participate per pull and how strongly.

| Variable | Default | Location | Rationale | Impact of Change |
|----------|---------|----------|-----------|------------------|
| `quality_good_threshold` | `0.62` | `...routing` | Boundary above which a pull is emotionally "good" and positive buckets activate. Set above 0.5 to require above-average pulls to feel rewarding. | **Higher:** Fewer pulls trigger positive routing; bar moves less. **Lower:** More pulls cause positive emotion; may feel too generous. |
| `quality_peak_threshold` | `0.85` | `...routing` | Top-tier threshold that triggers maximum rarity/pack routing weight (1.0 vs 0.6). Reserves the "Thrill" spike for truly exceptional pulls. | **Higher:** Peak moments are rarer and feel more special. **Lower:** Many decent pulls produce peak excitement, diluting the impact of genuinely great pulls. |
| `quality_bad_threshold` | `0.38` | `...routing` | Below this, negative rarity routing activates — the pull is bad relative to pack expectation. Asymmetrically tighter than good threshold to give a slight overall positive bias. | **Higher:** More pulls trigger negative routing; system feels more punishing. **Lower:** Fewer pulls cause disappointment — more forgiving but weakens the emotional feedback loop. |

#### Streak Parameters

| Variable | Default | Location | Rationale | Impact of Change |
|----------|---------|----------|-----------|------------------|
| `streak_window` | `5` | `...routing` | Rolling average of the last 5 pulls determines cold/hot mood. 5 balances responsiveness vs. noise — too short causes mood to flip every pull; too long makes mood feel unresponsive. | **Higher:** Streak mood is a long-term trend, immune to single outliers. **Lower:** Mood reacts immediately; can be volatile. |
| `cold_streak_threshold` | `0.40` | `...routing` | Average quality below this over the window = "cold mood." When a good pull breaks a cold streak, the streak bucket gets extra positive routing (Relief emotion). Set below 0.5 to only flag genuinely bad runs. | **Higher:** Cold mood triggers more easily; Relief boosts from decent pulls are more common. **Lower:** Requires a very bad run to flag cold mood; Relief is rare. |
| `hot_streak_threshold` | `0.60` | `...routing` | Average quality above this = "hot mood." Bad pulls during a hot streak hit the negative streak bucket (Letdown). Set above 0.5 to require a genuinely good run before bad pulls amplify disappointment. | **Higher:** Hot mood is harder to reach; Letdown effect is rarer. **Lower:** Players frequently enter hot mood; average bad pulls cause amplified disappointment. |

#### Economy / Value Score Parameters

| Variable | Default | Location | Rationale | Impact of Change |
|----------|---------|----------|-----------|------------------|
| `value_score_scale` | `1000.0` | `...routing` | Multiplier for `valueScore = (rawScore / cost) × scale`. Chosen to produce human-readable values in the ~1–5 range for typical packs, making threshold comparisons intuitive. | **Higher:** Scores are inflated; both value thresholds need scaling up proportionally. **Lower:** Scores cluster near zero; economy routing rarely triggers. |
| `value_good_threshold` | `2.20` | `...routing` | Value score above this means good bang for coins — triggers Worth/economy positive routing. Calibrated against Bronze pack (cost 1000, avg rawScore ~5 → valueScore ~5.0). | **Higher:** Economy positive routing only fires on exceptional value-for-cost pulls. **Lower:** Almost every acceptable pull triggers Worth; economy positive is too common. |
| `value_bad_threshold` | `1.80` | `...routing` | Value score below this (only for packs ≥ `high_cost_threshold_coins`) triggers Regret/negative economy routing. Gap between this and `value_good_threshold` is the neutral zone where neither fires. | **Higher:** Neutral zone shrinks; economy emotions fire more often. **Lower:** Wider neutral zone; fewer economy-based emotional reactions. |
| `high_cost_threshold_coins` | `1500` | `...routing` | Economy-regret routing only fires for expensive packs. Prevents Bronze pulls from ever triggering Regret — spending a little and getting commons should feel fine. | **Higher (e.g. 2000):** Only Gold packs trigger economy regret. **Lower (e.g. 1000):** Bronze packs can also trigger Regret on bad pulls — too punishing for low-investment packs. |

#### Routing Weight Summaries (Hardcoded logic mapping)
* Positive Base Weights:
  * `rarity_pack`: Peak = 1.0, Good = 0.6
  * `streak`: Cold mood relief = 0.9
  * `economy`: Good value = 0.6
* Negative Base Weights:
  * `rarity_pack`: Bad pull = 0.7
  * `streak`: Hot mood letdown = 0.9
  * `economy`: Bad value = 1.0

### 5.6 Phase 2 Decay Parameters

Defined in `phase2_config.json → phase_2_configuration.decay`. Applied **before** new deltas each pull. Lower value = faster fade. All bucket values clamped to `[0, 100]` after decay.

| Variable | Default | Location | Rationale | Impact of Change |
|----------|---------|----------|-----------|------------------|
| Positive `rarity_pack` decay | `0.985` | `...decay.positive` | Very slow fade (~1.5%/pull) — rarity excitement lingers across several pulls, rewarding good pack luck with a sustained mood boost. | **Lower (faster):** Thrill from a rare fades within a pull or two. **Higher (→1.0):** Rarity excitement never fades; bar stays elevated indefinitely. |
| Positive `streak` decay | `0.920` | `...decay.positive` | Fast fade (~8%/pull) — streak-based Relief is a momentary spike for breaking a cold streak, not a persistent state. | **Lower:** Streak boosts disappear almost immediately. **Higher:** Relief lingers even after streak context is gone, losing its emotional meaning. |
| Positive `economy` decay | `0.960` | `...decay.positive` | Moderate fade (~4%/pull) — "good deal" feeling is transient; fades over a few pulls. | **Lower:** Economy satisfaction is purely momentary. **Higher:** Players feel lasting satisfaction from any economically good pull regardless of what follows. |
| Negative `rarity_pack` decay | `0.985` | `...decay.negative` | Disappointment from a bad pull lingers similarly to Thrill — symmetry ensures neither family decays "conveniently" faster than the other. | Asymmetric values here would make negative emotions feel artificially short-lived or permanent relative to positive. |
| Negative `streak` decay | `0.920` | `...decay.negative` | Letdown (hot-streak-broken) fades quickly, matching the streak-context expiry — once the streak context changes, the emotion dissipates naturally. | Same tradeoffs as positive streak decay. |
| Negative `economy` decay | `0.960` | `...decay.negative` | Regret fades over a few pulls — high-cost disappointment should sting for a bit but not permanently sour an entire session. | **Lower:** Regret vanishes immediately. **Higher:** Player session is shadowed by regret for many subsequent pulls after one bad expensive opener. |

### 5.7 Phase 2 Recovery Parameters

Defined in `phase2_config.json → phase_2_configuration.recovery`. Controls cross-family dampening — good pulls reduce the negative bar and bad pulls reduce the positive bar.

| Variable | Default | Location | Rationale | Impact of Change |
|----------|---------|----------|-----------|------------------|
| `good_pull_reduces_negative` | `0.50` | `...recovery` | Good pulls actively push back on the negative bar at 50% of the positive delta applied, giving players a clear "things are improving" signal. | **Higher (e.g. 0.8):** Good pulls nearly erase negative emotion — very forgiving but reduces stakes of bad runs. **Lower (e.g. 0.2):** Negative bar is slow to recover even as luck improves. |
| `bad_pull_reduces_positive` | `0.50` | `...recovery` | Bad pulls cut into the positive bar at 50% of negative delta applied, keeping positive emotion honest — a bad pull after a lucky streak sets back some positivity. | **Higher:** Bad pulls heavily undercut positive emotion; the positive bar is fragile. **Lower:** Bad pulls barely dent positive; the green bar feels sticky and resilient regardless of outcomes. |
| `recovery.enabled` | `true` | `...recovery` | Master toggle for cross-family dampening. Disabling makes positive and negative bars fully independent — useful for isolated testing. | **Disabled:** Bars become uncorrelated; both can max out simultaneously, breaking the intended "tug-of-war" design. |

### 5.8 Duplicate XP

Defined in `phase2_config.json → duplicate_xp`. Controls how many XP points a player receives when opening a duplicate card.

| Variable | Default | Location | Rationale | Impact of Change |
|----------|---------|----------|-----------|------------------|
| `common_duplicate_xp` | `5` | `...duplicate_xp` | Minimal reward for duplicate Commons — they're very frequent by design, so XP must be low to avoid making duplicates a dominant progression path. | **Higher:** Farming commons for XP becomes attractive; may undermine card collection motivation. **Lower:** Duplicates feel completely worthless; bad for morale on unlucky sessions. |
| `uncommon_duplicate_xp` | `10` | `...duplicate_xp` | Moderate reward (2× common) reflecting slightly higher rarity. | **Higher/Lower:** Shifts relative value of building toward Uncommon-heavy pack strategies. |
| `rare_duplicate_xp` | `25` | `...duplicate_xp` | Meaningful reward (5× common) that softens the disappointment of drawing an already-owned Rare. | **Higher:** Duplicate Rares become genuinely desirable as an XP bonus. **Lower:** Rare duplicates feel purely wasteful. |
| `epic_duplicate_xp` | `50` | `...duplicate_xp` | Significant consolation (10× common) for a wasted Epic slot — keeps high-rarity duplicate pulls from feeling purely punishing. | **Higher:** Epic dupes worth chasing for XP farming. **Lower:** Epic duplicates still feel like a loss despite higher rarity. |
| `legendary_duplicate_xp` | `100` | `...duplicate_xp` | Maximum XP consolation (20× common). Legendaries are the most emotionally impactful duplicates, so the XP reward is proportionally large. | **Higher:** Legendary duplicates become a meaningful XP milestone. **Lower:** Even a wasted Legendary feels unrewarded — highest risk for player dissatisfaction. |

### 5.9 Hardcoded Constants & Inspector Fallbacks

These values are baked into the source code (`EmotionalStateManager.cs` or others) and are **not** overridable by JSON config. Changing them requires modifying the C# script.

#### Pack-Type Quality Bias & Expectations

| Variable | Default | Location | Rationale | Impact of Change |
|----------|---------|----------|-----------|------------------|
| Bronze bias exponent | `0.8` | `EmotionalStateManager.cs` | Bronze has the lowest expectation bar — pulls feel slightly better than raw score suggests, keeping new/casual players engaged. | **Closer to 1.0:** Bronze feels more neutral. **Further below 1.0:** Even all-commons feel acceptable in Bronze. Avoid below ~0.5 as it distorts quality perception too heavily. |
| Silver bias exponent | `1.0` | `EmotionalStateManager.cs` | Neutral baseline — Silver is the reference tier, no curve adjustment applied. | Changing this shifts the emotional baseline for all mid-tier pack comparisons across the system. |
| Gold bias exponent | `1.2` | `EmotionalStateManager.cs` | Players spending more coins expect better pulls; stricter curve means average Gold pulls feel mediocre and great pulls feel impactful. | **Higher (e.g. 1.5):** Gold feels very punishing for average pulls. **Lower (e.g. 1.0):** Removes the premium expectation gap between Silver and Gold emotionally. |
| Bronze rarity special threshold | `≥ 3` | `EmotionalStateManager.cs` | Rare or better considered "special" for Bronze and triggers peak routing. | Changing affects Thrill triggers for low-tier packs. |
| Silver rarity special threshold | `≥ 4` | `EmotionalStateManager.cs` | Epic or better considered "special" for Silver. | Changing affects Thrill triggers for mid-tier packs. |
| Gold rarity special threshold | `≥ 5` | `EmotionalStateManager.cs` | Legendary only considered "special" for Gold. | Changing affects Thrill triggers for premium packs. |

#### Other Hardcoded Formulas
* **Positive curve exponent**: `0.7` (`pow(quality01, 0.7)`). Makes positive emotions easier to gain at lower quality.
* **Negative curve exponent**: `1.2` (`pow(1−quality01, 1.2)`). Makes negative emotions harder to gain at borderline quality.
* **Rare boost multiplier**: `1.15` (Applied to `dP` when Rare/Epic/Legendary present).
* **Hot mood negative streak quality ceiling**: `0.5` (quality01 must be ≤ 0.5 for neg streak to apply).
* **Bucket value clamp**: `[0, 100]` (All bucket values are clamped to this range).

#### Inspector-Exposed Fallbacks (EmotionalStateManager)
Only used if no JSON config is loaded (i.e. missing or malformed).
* `P_max_Fallback`: `3.0`
* `N_max_Fallback`: `2.0`
* `verbose`: `true`

### 5.10 Legacy Phase 1 Emotion Dynamics

Defined in `phase2_config.json → emotion_dynamics`. These were active in Phase 1 and are retained in the config for backwards compatibility. The Phase 2 `EmotionalStateManager` does **not** use these fields directly in its main pipeline.

| Variable | Default   | Description                     |
| -------- | --------- | ------------------------------- |
| `S_max`  | **3.0**   | Max satisfaction delta per pull |
| `F_max`  | **2.0**   | Max frustration delta per pull  |
| `S_cap`  | **100.0** | Satisfaction ceiling            |
| `F_cap`  | **100.0** | Frustration ceiling             |
| `enabled` (Quality Reset) | `true` | Toggle for quality-driven reduction |
| `R_F` (Quality Reset) | `1.5` | Frustration reduction amount for good pulls |
| `neutral_band.enabled` | `true` | Toggle for neutral-band recovery step |
| `neutral_band.min` | `0.38` | Lower quality01 bound |
| `neutral_band.max` | `0.62` | Upper quality01 bound |
| `oppositional.enabled` | `true` | Toggle for oppositional coupling |
| `oppositional.k` | `0.25` | Cross-dampening coefficient (hard-reduced by factor of `0.3` in code) |
| `streak.window` | `5` | Number of past pulls |
| `streak.alpha` | `1.0` | Satisfaction amplification (halved in code) |
| `streak.beta` | `1.0` | Frustration amplification (halved in code) |

---

## 6. Location Legend

- **Config:** JSON in `StreamingAssets/CCAS/phase2_config.json` — edit and relaunch to take effect
- **File:** Hardcoded in C# source — requires code change and Unity recompile
- **Env:** Environment variable
- **DB:** Database / remote config

## 7. Change Impact Summary & Quick-Tuning Guide

- **Emotion feel:** `P_max`, `N_max`, pack bias exponents, and curve exponents are the primary levers for broad feel changes.
- **Routing behavior:** Quality/streak/economy thresholds determine *when* each bucket activates. Adjusting these shifts which player actions "count" emotionally.
- **Session arc:** Decay and recovery values shape how emotions evolve *across* a session — fast decay makes each pull standalone; slow decay builds a session-wide emotional narrative.
- **Economy dimension:** Value score thresholds and `high_cost_threshold_coins` exclusively govern the economy bucket and affect how much players feel the cost-vs-reward of pack purchases.
- **Duplicate fairness:** Duplicate XP is a pure progression/economy lever.

#### Quick Tuning Scenarios
* **Positive Emotions Feel Too Strong:** Lower `P_max` (e.g. 3.0 → 2.0), Raise `quality_good_threshold`, Reduce `good_pull_reduces_negative`.
* **Positive Emotions Feel Too Weak:** Raise `P_max` (e.g. 3.0 → 4.0), Lower `quality_good_threshold`, Raise `good_pull_reduces_negative`.
* **Negative Emotions Feel Too Punishing:** Lower `N_max` (e.g. 2.0 → 1.5), Lower `quality_bad_threshold`, Raise `hot_streak_threshold`.
* **Negative Emotions Recover Too Fast:** Lower `decay.negative.rarity_pack` (e.g. 0.985 → 0.995 to slow decay), Lower `good_pull_reduces_negative`.
* **Streaks Feel Too Impactful:** Raise `cold_streak_threshold` and `hot_streak_threshold` closer together (narrower neutral zone), Reduce `streak_window`.
* **Economy Routing Fires Too Often / Rarely:** Adjust `value_good_threshold` and `value_bad_threshold`, Adjust `value_score_scale`.
* **Rare Cards Feel Underwhelming:** Increase the rare boost multiplier (currently **1.15** in code), Lower the Silver/Gold rarity specialness thresholds.
