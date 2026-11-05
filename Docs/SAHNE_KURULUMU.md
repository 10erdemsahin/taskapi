# Sahne Kurulumu (Elle, Adım Adım)

> Bu belge eski README'den taşındı. Sahneyi elle kurmak yerine Unity'de **Tools → AdventureGame → 2) Sahneyi Oluştur** (oyun sahnesi) ve **3) Ana Menü Sahnesini Oluştur** (menü) araçları her şeyi otomatik kurar.
> Proje kurulumu için: [../KURULUM.md](../KURULUM.md) · Ana rapor: [../README.md](../README.md)

---

## 1. Ön Hazırlık

### 1.1 Unity
* **Unity 2022.3 LTS** veya **Unity 6** · Şablon: **3D (Built-in)** ya da **3D (URP)**.

### 1.2 Paketler (Window → Package Manager)
| Paket | Nasıl |
|---|---|
| **AI Navigation** (`com.unity.ai.navigation`) | Unity Registry → AI Navigation → Install |
| **Newtonsoft Json** (`com.unity.nuget.newtonsoft-json`) | `+` → *Add package by name…* → `com.unity.nuget.newtonsoft-json` |
| **TextMeshPro** | İlk TMP objesi eklenince çıkan pencerede **Import TMP Essentials** |

### 1.3 Project Settings
1. **Player → Other Settings → Active Input Handling** → **Both** (veya *Input Manager (Old)*).
   Unity 6'da varsayılan “Input System Package (New)” ise `Input.GetAxis` hata verir.
2. **Player → Other Settings → Allow downloads over HTTP** → **Always allowed**
   (Ollama `http://` kullanır; Unity 2022+ güvensiz HTTP'yi varsayılan olarak engeller).
3. **Tags and Layers**: Layer ekleyin → `Interactable`.

### 1.4 Ollama
```bash
ollama pull qwen2.5:7b
```
```bash
ollama serve
```
```bash
curl http://localhost:11434/api/tags
```
* Windows'ta Ollama uygulaması açıksa `ollama serve` zaten çalışıyordur.
* Türkçe ve JSON uyumu için önerilen: `qwen2.5:7b`. Zayıf donanımda: `llama3.2:3b` (Inspector'dan `model` alanını değiştirin).
* İlk istek modeli belleğe yükler (10–30 sn sürebilir); `requestTimeoutSeconds = 60` bu yüzden yüksektir.

---

## 2. Sahne Kurulumu

> Tüm koordinatlar dünya koordinatıdır. Küpler için **GameObject → 3D Object → Cube**.
> Zemin üstü y = 0 kabul edilmiştir.

### 2.1 Zemin ve Uçurum
| Obje | Position | Scale | Not |
|---|---|---|---|
| `Ground_A` (Cube) | (0, -0.5, 0) | (20, 1, 20) | Başlangıç alanı, z: -10…10 |
| `Ground_B` (Cube) | (0, -0.5, 27) | (20, 1, 20) | Karşı alan, z: 17…37 → **7 m boşluk** |
| `HighPlatform` (Cube) | (-7, 1.5, 5) | (4, 3, 4) | Üstü y = 3 (oyuncu zıplama 1.1 m) |

### 2.2 Şalter (platform üstünde)
1. Boş obje `Lever` → Position **(-5.6, 3, 5)**, Layer **Interactable**.
   * `Box Collider` ekle: Center (0, 0.6, 0), Size (0.6, 1.2, 0.6) — **Is Trigger kapalı**.
   * **LeverSwitch** ekle (Reset otomatik `allowPlayer = false` yapar).
2. Çocuk `Base` (Cube): local (0, 0.1, 0), scale (0.5, 0.2, 0.5) → **Box Collider'ını silin**.
3. Çocuk boş `HandlePivot`: local (0, 0.2, 0) → onun çocuğu `Handle` (Cube): local (0, 0.5, 0), scale (0.12, 1, 0.12) → collider'ı silin.
4. Çocuk boş `LeverAnchor`: local **(-1, 0, 0)** (yoldaşın duracağı nokta, platform üstünde).
5. LeverSwitch alanları: `Interaction Anchor = LeverAnchor`, `Handle Pivot = HandlePivot`, `Target Behaviour = BridgePivot` (2.3).

### 2.3 Asma Köprü
1. Boş obje `BridgePivot` → Position **(0, 0, 17)**, **Rotation (90, 0, 0)** (kalkık!). **DrawBridge** ekle.
2. Çocuk `Deck` (Cube): local (0, -0.15, -3.6), scale (3, 0.3, 7.4).
3. DrawBridge: `Pivot = BridgePivot`, `Raised = (90,0,0)`, `Lowered = (0,0,0)`, `Nav Mesh Surface = NavMesh` (2.7).

> ⚠ NavMesh **köprü kalkıkken** bake edilmeli; aksi halde yoldaş köprü varmış gibi davranır.

### 2.4 Duvar, Kapı, Muhafız, Bitiş
| Obje | Position | Scale/Rotation |
|---|---|---|
| `Wall_L` (Cube) | (-6, 2, 30) | (8, 4, 1) |
| `Wall_R` (Cube) | (6, 2, 30) | (8, 4, 1) |
| `Lintel` (Cube) | (0, 3.5, 30) | (4, 1, 1) |
| `DoorHinge` (boş) | (-2, 0, 30) | **InteractiveDoor** + **NavMeshModifier** (Ignore From Build ✓) |
| └ `DoorPanel` (Cube) | local (2, 1.5, 0) | (4, 3, 0.3) + **NavMeshObstacle** (Carve ✓) |
| `GuardNPC` (Capsule) | (3.5, 1, 28.5) | Rotation (0, 180, 0), Layer **Interactable**, **GuardNPC** |

InteractiveDoor: `Hinge = DoorHinge`, `Open Local Euler = (0, -100, 0)` (kapı +z yönüne, içeri açılır).

### 2.5 Tetik Bölgeleri (hepsi Layer = **Ignore Raycast**, Box Collider **Is Trigger ✓**)
| Obje | Position | Scale | Bileşen ve Bağlantı |
|---|---|---|---|
| `ObstacleZone` | (0, 1, 8) | (20, 2, 2.5) | PlayerTriggerZone → On Player Enter: `GameManager.NotifyObstacleReached` |
| `GoalZone` | (0, 1.5, 33.5) | (4, 3, 3) | PlayerTriggerZone → On Player Enter: `GameManager.CompleteGame` |
| `KillZone` | (0, -8, 13) | (200, 2, 200) | RespawnZone → Respawn Point = `RespawnPoint` |
| `RespawnPoint` (boş) | (0, 1, -6) | – | – |

> UnityEvent bağlama: `+` → `GameManager` objesini sürükle → fonksiyon listesinden `GameManager → NotifyObstacleReached()`.

### 2.6 NavMeshLink (yoldaşın platforma zıplaması)
1. Boş obje `PlatformJumpLink` → Position **(0, 0, 0)**, Rotation 0 (yerel = dünya koordinatı olsun).
2. **NavMesh Link** bileşeni: Start Point **(-3.5, 0, 5)**, End Point **(-7.2, 3, 5)**, Width **1.5**,
   **Bidirectional ✓**, Area Type **Walkable**.
3. Oyuncu bu linki kullanamaz çünkü CharacterController ile yürür, NavMesh kullanmaz → fiziksel engel gerçek kalır.

### 2.7 NavMesh
1. Boş obje `NavMesh` → **NavMeshSurface**:
   * Agent Type: Humanoid · Collect Objects: **All Game Objects**
   * Include Layers: **Everything hariç `Ignore Raycast`** (tetik bölgeleri bake edilmesin)
   * Use Geometry: **Physics Colliders**
2. Player ve Companion objelerine **NavMeshModifier → Ignore From Build ✓** ekleyin.
3. Köprü kalkıkken **Bake**. Scene görünümünde mavi alan: A zemini, B zemini, platform üstü; köprü yok. Link oku platforma gitmeli.

### 2.8 Oyuncu
1. `Player` (Capsule) → Position (0, 1, -6), Tag **Player**. **Capsule Collider'ı silin.**
2. **Character Controller**: Center (0,0,0), Height 2, Radius 0.4.
3. `Main Camera`'yı Player'ın çocuğu yapın: local (0, 0.7, 0), rotation 0.
4. **PlayerController**: `Camera Pivot = Main Camera`.
5. **PlayerInteractor**: `View Camera = Main Camera`, `Interactable Mask = Interactable`.

### 2.9 Yoldaş
1. `Companion` (Capsule) → Position (2, 1, -5), farklı renkte materyal.
2. **NavMeshAgent**: Base Offset **1**, Radius 0.4, Height 2, Speed 4.5, Angular Speed 360.
3. Çocuk boş `Eye`: local (0, 0.6, 0.35).
4. **CompanionSensor**: `Eye = Eye`, `Interactable Mask = Interactable`, `Occluder Mask = Default`, Scan Radius 25.
5. **NavMeshCompanionController**: Display Name “Robo” (diğer değerler varsayılan).

### 2.10 Servisler ve GameManager
1. Boş obje `Services` → **OllamaLLMService** (model: `qwen2.5:7b`). Test için ayrıca **MockLLMService** eklenebilir.
2. Boş obje `GameManager` → **GameManager**:

| Alan | Atanacak obje |
|---|---|
| Player | `Player` |
| Companion Behaviour | `Companion` |
| Bridge Behaviour | `BridgePivot` |
| Npc Behaviour | `GuardNPC` |
| Door Behaviour | `DoorHinge` |

> GameObject'i sürüklemeniz yeterli; `InterfaceResolver` doğru bileşeni bulur.

### 2.11 Persona
Project → sağ tık → **Create → AdventureGame → NPC Persona** → `GuardPersona`.
Bilmece, kurallar ve yedek cümleler hazır gelir; isterseniz düzenleyin.
`GuardNPC` alanları: `Persona = GuardPersona`, `Llm Service Behaviour = Services`, `Dialogue View Behaviour = Canvas`.

---

## 3. UI Kurulumu

`GameObject → UI → Canvas` (Screen Space – Overlay, Canvas Scaler: *Scale With Screen Size* 1920×1080). EventSystem otomatik gelir.
**Canvas** objesine **DialogueUI** ve **GameHUD** ekleyin (Canvas hep aktif kalır).

```
Canvas  [DialogueUI, GameHUD]
├── HUD
│   ├── ObjectiveText        (TMP, sol üst)
│   ├── LlmStatusText        (TMP, sağ üst, font 20)
│   ├── Crosshair            (TMP "+", merkez)
│   ├── PromptText           (TMP, merkezin biraz altı)
│   └── Notification         [CanvasGroup]
│       └── NotificationText (TMP, üst orta)
├── DialoguePanel            (Image, alt-orta, ~1100×600, yarı saydam)  ← panelRoot
│   ├── TitleText            (TMP)
│   ├── ScrollView           (UI → Scroll View; yatay kaydırmayı kapatın)
│   │   └── Viewport/Content [Vertical Layout Group (Control Child Size: Height ✓),
│   │                         Content Size Fitter (Vertical: Preferred)]
│   │       └── LogText      (TMP, Wrapping açık, Rich Text ✓)
│   ├── ThinkingIndicator    (boş obje)
│   │   └── ThinkingLabel    (TMP, italik: "Muhafız düşünüyor...")
│   ├── InputField           (UI → Input Field - TextMeshPro, Line Type: Single Line)
│   ├── SendButton           (Button - TMP, "Gönder")
│   └── CloseButton          (Button - TMP, "X")
└── VictoryPanel             (Image tam ekran, başlık "Tebrikler!")
    ├── RestartButton
    └── QuitButton
```

**DialogueUI alanları:** Panel Root = DialoguePanel · Title Text · Scroll Rect = ScrollView · Log Text · Input Field · Send Button · Close Button · Thinking Indicator · Thinking Label.

**GameHUD alanları:** Objective Text · Notification Text · Notification Group = Notification · Player Interactor = Player · Interaction Prompt Text = PromptText · Crosshair · Llm Service Behaviour = Services · Llm Status Text · Victory Panel · Restart/Quit Button.

> Restart butonunun çalışması için sahneyi **File → Build Settings → Add Open Scenes** ile ekleyin.


---

## 4. Sorun Giderme

| Belirti | Çözüm |
|---|---|
| `InvalidOperationException: You are trying to read Input using the UnityEngine.Input class` | Active Input Handling → **Both** |
| `Insecure connection not allowed` | Allow downloads over HTTP → **Always allowed** |
| `The type or namespace 'Newtonsoft' could not be found` | Newtonsoft Json paketini ekleyin |
| `Unity.AI.Navigation` bulunamadı | AI Navigation paketini ekleyin |
| Yoldaş “Görebildiğim… mekanizma yok” diyor | Lever Layer = Interactable mı? Sensor mask'ları doğru mu? Platforma daha yakın durun. |
| Yoldaş platforma çıkmıyor | NavMeshLink uç noktaları mavi alanın içinde mi? Bake sonrası link oku görünmeli. |
| Yoldaş köprüden geçmiyor | DrawBridge'e NavMeshSurface atandı mı? Köprü kalkıkken mi bake edildi? |
| `localhost` bağlanmıyor | `baseUrl = http://127.0.0.1:11434` deneyin |
| NPC hep İngilizce / kurala uymuyor | Daha güçlü model (`qwen2.5:7b`, `llama3.1:8b`), temperature 0.3–0.5 |
| IL2CPP build'de JSON boş geliyor | Managed Stripping Level → Minimal veya `link.xml` ile Newtonsoft'u koruyun |
