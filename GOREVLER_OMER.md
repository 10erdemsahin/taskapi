# Ömer'in Görevleri — Görsel, Menü, Toplama Görevi, Doküman

Önce [KURULUM.md](KURULUM.md)'yi baştan sona uygula. Genel plan: [YAPILACAKLAR.md](YAPILACAKLAR.md).
Erdem'in görevleri: [GOREVLER_ERDEM.md](GOREVLER_ERDEM.md).

**Repo:** https://github.com/10erdemsahin/taskapi

## Senin Alanın (sadece bunlara dokunursun)

| Sahibi olduğun | Dokunmayacağın (Erdem'in) |
|---|---|
| `Assets/Art/**` (texture, skybox, prefab, model, ses) | `Assets/Scenes/Main.unity` (açıp oynatabilirsin, **kaydetmezsin**) |
| `Assets/Materials/**` | `Assets/Scripts/**` (**`UI/` ve `Quest/` hariç**) — özellikle `Core/`, `GameManager.cs` |
| `Assets/Scripts/UI/**` (DialogueUI, GameHUD, menüler) | `Assets/Editor/**`, `Assets/Tests/**` |
| **`Assets/Scripts/Quest/**`** (toplama görevi — YENİ) | `Assets/Data/**` (**`Data/Quest/` hariç**) |
| **`Assets/Data/Quest/**`** (ipucu verisi — YENİ) | `ProjectSettings/` (**Build Settings hariç**), `Packages/` |
| `Assets/Scenes/MainMenu.unity` | |
| `ProjectSettings/EditorBuildSettings.asset` (sahne sırası) | |
| `README.md`, `Docs/**` | |

> **Main.unity'yi yanlışlıkla kaydettiysen** commit'lemeden geri al: `git checkout -- Assets/Scenes/Main.unity`
> Sahnede bir değişiklik gerekiyorsa (materyal atama, buton ekleme, prefab yerleştirme) **Erdem'e yaz**, o yapar.

**Bozulmayacak kurallar:**
- `DialogueUI`'nin "düşünüyor..." göstergesi ve LLM beklerken girişin kilitlenmesi **kaldırılmaz** (ders isteri).
- `DialogueUI` yalnızca gösterir; NPC/LLM mantığı UI'ye taşınmaz.
- Toplama sistemi GameManager'ı **doğrudan çağırmaz**; sadece `ICollectionQuest` olaylarını yayınlar (GameManager dinler).
- Menüden sahne yüklemeden önce **`Time.timeScale = 1f`** yapılır.

---

## Adım 0 — Hazırlık ✅

- [x] Kurulum, clone, ilk commit'ler.
- [ ] **5 Kasım commit temizliği sonrası senkron** (Erdem'in mesajındaki komutlar) — yapılmadıysa önce bunu yap.
- [ ] Her işe başlarken: `git checkout main` → `git pull` → `git checkout -b omer/<iş-adı>`

---

## Adım 1 — Texture Pack ✅ (PR #1 ile main'de)

- [x] ambientCG CC0 1K texture'lar (Ground068, Bricks113A, Planks037A, Metal021), materyaller, TempleSky skybox materyali.
- [x] `Assets/Art/LISANSLAR.txt` — var; yeni eklediğin her asset'i (ses, model, duvar texture'ı) buraya da yaz.
- [ ] **Skybox'ı sahneye Erdem atayacak** (sahne onda) — sen bir şey yapmana gerek yok.

### 1.6 Duvar materyali — `Assets/Materials/Wall.mat` (YENİ, 1. gün)
Erdem haritanın yan ve arka kenarlarına 3 m yüksekliğinde, 21–22 m uzunluğunda duvarlar ekliyor. `Stone.mat` (tiling 2,2) bu boyutta **çok uzar**.
- [ ] Dal: `omer/duvar-materyal`
- [ ] Yeni materyal `Wall.mat`, Shader Standard, mevcut taş/tuğla texture'ı (Bricks113A) veya daha koyu, yosunlu bir taş (ambientCG, 1K).
- [ ] **Tiling: (10, 1.5)** başlangıç; Erdem'in duvarları gelince oyunda bakıp ayarla.
- [ ] Albedo rengi **beyaz**, Normal Map + Occlusion bağlı.
- [ ] PR → Erdem'e: "`Wall.mat` hazır, duvarlara atar mısın?"

### 1.7 Korkuluk görseli — düşmeyi engelleme (1.–2. gün, YENİ)
Erdem uçurum kenarına ve köprünün iki yanına **görünmez 3 m'lik çarpışma duvarları** koyuyor (oyuncu hiçbir yerden düşemesin). Oyuncu bunların orada olduğunu görebilsin diye **görsel korkuluk** senin işin.
- [ ] Dal: `omer/korkuluk`
- [ ] `Assets/Art/Prefabs/StoneRailing.prefab`: **2 m uzunluk, 1 m yükseklik, ~0.3 m kalınlık** taş korkuluk modülü (alçak duvar + üst kapak taşı **veya** küçük sütunlar + üst kiriş). Yan yana dizilince birleşik görünmeli (uçların hizalı olması).
  - Pivot: **alt-orta** (y = 0 zemin) → Erdem kolayca dizebilsin.
- [ ] `Assets/Art/Prefabs/BridgeRailing.prefab`: ahşap köprü korkuluğu, **1 m uzunluk, 1 m yükseklik**, `Wood.mat` (köprü boyunca 7–8 tane dizilecek).
- [ ] Materyal `Assets/Materials/Railing.mat` (Bricks/Stone texture, tiling ~2×1).
- [ ] Prefab'larda **sadece görsel**: collider **yok**, script yok (çarpışmayı Erdem'in görünmez duvarları yapıyor; çift collider oyuncuyu takabilir).
- [ ] Kontrol: `Main.unity`'ye geçici olarak birkaç tane koyup bak → **kaydetmeden kapat**.
- [ ] PR → Erdem'e: "Korkuluk prefab'ları hazır: `StoneRailing`, `BridgeRailing`."

---

## Adım 2 — Ana Menü 🟡 (kod hazır, eksikler var)

Dal: `omer/ana-menu` (5 Kasım temizliğinden sonraki hâliyle devam et)

- [x] `MainMenuUI.cs`, `GameSettings.cs`, `MenuCameraOrbit.cs`, `MainMenuBuilder.cs` (editör aracı)
- [x] `GameHUD`: `mainMenuButton` alanı + `GoToMainMenu()` + `Time.timeScale = 1f`
- [ ] **`Assets/Scenes/MainMenu.unity` + `.meta` commit'lenmedi** → aracınla sahneyi oluştur, kaydet, commit'le.
- [ ] **Build Settings:** 0: `MainMenu`, 1: `Main` → `ProjectSettings/EditorBuildSettings.asset` commit'le.
- [ ] EventSystem'de **Standalone Input Module** olduğundan emin ol.
- [ ] Türkçe karakterler (ı, ş, ğ, İ) düzgün görünüyor mu?
- [ ] **PR aç:** `omer/ana-menu` → `main` (Draft değil). PR açıklamasına "Build Settings değişti" yaz.
- [ ] Zafer panelindeki **buton objesini sahneye Erdem ekleyecek**; sen kodu bıraktın, yeterli.

### Kabul ölçütleri
- [ ] Menü → Başla → oyun açılıyor, fare kilitleniyor.
- [ ] Ayarlardan hassasiyet değişince oyunda fare hızı değişiyor (Erdem'in `erdem/duraklatma-api` PR'ı main'e girince).
- [ ] Ses ayarı kalıcı. Console'da hata yok.

---

## Adım 2B — Toplama Görevi: "Yankı Taşı Parçaları" (2.–3. gün, YENİ)

Dal: `omer/toplama-gorevi`. **Önkoşul:** Erdem'in `ICollectionQuest` arayüzü main'de olmalı (1. gün). Arayüz:
```csharp
public interface ICollectionQuest
{
    int CollectedCount { get; }
    int RequiredCount { get; }
    bool IsCompleted { get; }
    string HintText { get; }
    event Action<int, int> ProgressChanged;  // (toplanan, gereken)
    event Action<string> Completed;          // ipucu metniyle, bir kez
}
```

**Kurgu:** Tapınağın 5 Yankı Taşı parçası haritaya dağılmış. Hepsi toplanınca taşlar birleşir ve oyuncuya **muhafızın bilmecesi için ipucu** fısıldar. Parçalar tamamlanmadan muhafız kapıyı açmaz (bu kuralı Erdem GameManager'da uygular).

### 2B.1 Toplanabilir parça — `Assets/Scripts/Quest/Collectible.cs`
- [ ] `public sealed class Collectible : InteractableBase` (namespace `AdventureGame`) — mevcut şalterle aynı taban sınıf; böylece hem oyuncunun [E]'si hem yoldaşın sensörü **otomatik** çalışır.
- [ ] Alanlar: `string shardId`, `GameObject visualRoot`, `ParticleSystem pickupEffectPrefab`, `AudioClip pickupSound`.
- [ ] `public event Action<Collectible> Collected;`
- [ ] `protected override bool IsAvailable => !_collected;` (toplanan parça ikinci kez alınmasın)
- [ ] `OnInteract`: `_collected = true` → efekt + ses (`AudioSource.PlayClipAtPoint`) → `Collected?.Invoke(this)` → collider'ları kapat, `visualRoot`'u gizle (objeyi hemen `Destroy` etme; yoldaş o karede hâlâ referans tutuyor olabilir).
- [ ] `Reset()`: `interactionPrompt = "Yankı Taşı parçasını al [E]"`.
- [ ] **Yoldaş parçası:** platformdaki parça için Inspector'da `allowPlayer = false`, `allowAICompanion = true` (Erdem sahnede ayarlar; sen alanların çalıştığından emin ol).

### 2B.2 Görsel — `Assets/Scripts/Quest/FloatingSpin.cs` + prefab
- [ ] `FloatingSpin`: parçayı yavaşça döndür + yukarı aşağı salla (`Mathf.Sin`). Sadece `visualRoot`'u hareket ettir, collider sabit kalsın.
- [ ] Materyal `Assets/Materials/EchoShard.mat`: Standard, **Emission açık** (mavi/turkuaz parıltı).
- [ ] Prefab `Assets/Art/Prefabs/EchoShard.prefab`:
```
EchoShard            [Collectible, SphereCollider r=0.4 (Is Trigger KAPALI), Layer: Interactable]
└── Visual           [FloatingSpin]
    ├── Crystal      (küçük kristal model veya döndürülmüş, ölçeklenmiş küp; EchoShard.mat)
    └── Glow         (Point Light, Range 3, Intensity 1.5, turkuaz) — isteğe bağlı
```
  > Collider **trigger olmamalı**: oyuncunun ışını ve yoldaşın sensörü trigger'ları yok sayar.
- [ ] Toplama efekti: küçük bir Particle System prefab'ı (`Assets/Art/Prefabs/ShardPickupFX.prefab`), 1 sn sonra kendini yok etsin.
- [ ] Ses (CC0, ör. kenney.nl "Interface Sounds" / "Sci-fi Sounds"): `Assets/Art/Audio/shard_pickup.ogg` + `LISANSLAR.txt`'ye ekle.

### 2B.3 İpucu verisi — `Assets/Scripts/Quest/QuestHintData.cs` + asset
- [ ] `[CreateAssetMenu(menuName = "AdventureGame/Quest Hint")] public sealed class QuestHintData : ScriptableObject` — alanlar: `questName`, `[TextArea] hintText`, `[TextArea] completionMessage`.
- [ ] Asset: `Assets/Data/Quest/EchoStoneHint.asset`
  - `questName`: "Yankı Taşı"
  - `completionMessage`: "Beş parça birleşti. Taş titreyerek fısıldıyor..."
  - `hintText` (cevabı **söylemeyen**, yön gösteren bir ipucu yaz; örnek):
    > "Bu tapınakta en değerli şey bir ses değil, sesin yokluğudur. Muhafızın sorusuna kelimeyle değil, kelimelerin bittiği yerle cevap ver."
  - ⚠ İpucunda cevap kelimesi (`sessizlik`, `sükût`) **geçmesin**.

### 2B.4 Görev sayacı — `Assets/Scripts/Quest/CollectionQuest.cs`
- [ ] `public sealed class CollectionQuest : MonoBehaviour, ICollectionQuest`
- [ ] `[SerializeField] private List<Collectible> collectibles;` `[SerializeField] private QuestHintData hintData;`
- [ ] `RequiredCount => collectibles` içindeki **null olmayan** parça sayısı.
- [ ] `OnEnable`: her parçanın `Collected` olayına abone ol; `OnDisable`: aboneliği bırak.
- [ ] Toplanan parçaları `HashSet<Collectible>`'da tut → aynı parça iki kez sayılmasın.
- [ ] Her toplamada `ProgressChanged?.Invoke(CollectedCount, RequiredCount)`; sayı tamamlanınca **bir kez** `Completed?.Invoke(HintText)`.
- [ ] `HintText => hintData != null ? hintData.hintText : ""` — `hintData` yoksa hata loglayıp boş metin dön, **çökmesin**.
- [ ] `collectibles` boşsa veya null eleman varsa `Debug.LogWarning` (NullReferenceException yok).

### 2B.5 HUD — `Assets/Scripts/UI/GameHUD.cs` (senin dosyan)
- [ ] Yeni alanlar: `[SerializeField] MonoBehaviour questBehaviour;` (→ `InterfaceResolver.Resolve<ICollectionQuest>`), `TMP_Text questCounterText`, `GameObject hintPanel`, `TMP_Text hintText`.
- [ ] Sayaç: sağ üstte, LLM durumunun altında **"Yankı Taşı: 0/5"**; tamamlanınca yeşil "Yankı Taşı: 5/5 ✓".
- [ ] `Completed` gelince ipucu paneli açılır (ekran ortası, yarı saydam, italik metin), **8 sn** sonra kapanır (`WaitForSecondsRealtime`).
- [ ] Oyuncu ipucunu tekrar görebilsin: **[H]** tuşu paneli 8 sn yeniden açar (sadece görev tamamlandıysa, girdi kilitli değilse).
- [ ] Erdem'e yaz: "Canvas'a şu objeler lazım: `QuestCounterText`, `HintPanel/HintText`; GameHUD alanlarına ve `questBehaviour`'a `CollectionQuest`'i bağlar mısın?" (veya bu objeleri bir **prefab** olarak ver: `Assets/Art/Prefabs/QuestHUD.prefab`).

### 2B.6 Kendi testin
- [ ] `Main.unity`'yi aç → `EchoShard` prefab'ından 2–3 tane koy + boş obje üzerine `CollectionQuest` → bağla → Play → topla → sayaç, efekt, ses, ipucu paneli, [H] çalışıyor mu → **sahneyi KAYDETMEDEN kapat**.
- [ ] Yoldaşla dene: bir parçayı `allowPlayer = false` yap, yoldaşın göreceği yere koy, [F] → yoldaş gidip alıyor mu?
- [ ] Console'da kırmızı hata yok.

### 2B.7 Teslim
- [ ] PR: `omer/toplama-gorevi` → `main`. Açıklamaya: dosya listesi + "sahneye yerleştirme Erdem'de" + Canvas'a gereken objeler.
- [ ] Erdem bu PR'dan sonra parçaları sahneye yerleştirip kapıya bağlayacak ve test yazacak.

### Kabul ölçütleri
- [ ] Parça [E] ile toplanıyor, kayboluyor, efekt + ses çalıyor, sayaç artıyor.
- [ ] Aynı parça iki kez sayılmıyor; 5/5 olunca ipucu **bir kez** çıkıyor; [H] ile tekrar görülebiliyor.
- [ ] Yoldaş, kendisine ayrılmış parçayı [F] ile alabiliyor.
- [ ] `hintData` / parça eksik olsa bile oyun çökmüyor.

---

## Adım 3 — Duraklatma Menüsü (4. gün)

Dal: `omer/duraklatma`. **Önkoşul:** Erdem'in `erdem/duraklatma-api` PR'ı main'de olmalı (kod hazır, test aşamasında). Hazır olan API:
- `GameManager.Instance.SetPaused(bool)` — zamanı durdurur, girdiyi kilitler, imleci serbest bırakır
- `GameManager.Instance.IsPaused`, `GameManager.Instance.PausedChanged` (`Action<bool>`)
- `PlayerController.SetMouseSensitivity(float)` — ayarı anında uygular; `PlayerController.MouseSensitivityKey == "MouseSensitivity"` (senin `GameSettings.SensitivityKey` ile aynı)

### 3.1 Prefab
- [ ] `Main.unity`'yi aç → Canvas'ın altına `PauseMenu` paneli kur: yarı saydam siyah arka plan + "Duraklatıldı" + **Devam Et / Ayarlar / Ana Menü / Çıkış**. Ayarlar paneli ana menüdekiyle aynı slider'lar.
- [ ] Paneli `Assets/Art/Prefabs/PauseMenu.prefab` olarak sürükleyip prefab yap → sahneden **sil** → **sahneyi kaydetme**.
- [ ] Erdem'e: "PauseMenu prefab hazır, Canvas altına koyar mısın?"

### 3.2 Script — `Assets/Scripts/UI/Menu/PauseMenuUI.cs`
- [ ] **Esc** ile aç/kapat → `GameManager.Instance.SetPaused(true/false)`.
- [ ] `PausedChanged`'e abone ol → paneli göster/gizle. `OnDestroy`'da aboneliği bırak.
- [ ] **ÇOK ÖNEMLİ — Esc çakışması:** Esc diyalog penceresini de kapatıyor. Diyalog kapanırken aynı karede duraklatma menüsü **açılmamalı**:
```csharp
private bool _wasLockedLastFrame;

private void Update()
{
    GameManager gm = GameManager.Instance;
    if (gm == null || gm.Phase == GamePhase.Completed) return;

    if (Input.GetKeyDown(KeyCode.Escape))
    {
        if (gm.IsPaused) gm.SetPaused(false);
        else if (!gm.IsInputLocked && !_wasLockedLastFrame) gm.SetPaused(true); // diyalog açıktı → açma
    }
}

private void LateUpdate() => _wasLockedLastFrame = GameManager.Instance != null && GameManager.Instance.IsInputLocked && !GameManager.Instance.IsPaused;
```
- [ ] Duraklatılmışken çalışacak her animasyon **unscaled** olmalı (`WaitForSecondsRealtime`, `Time.unscaledDeltaTime`).
- [ ] **Ana Menü** butonu: `gm.SetPaused(false); Time.timeScale = 1f; SceneManager.LoadScene(MainMenuUI.MenuSceneName);`
- [ ] Ayarlar: `GameSettings` sınıfını kullan; hassasiyet değişince `FindAnyObjectByType<PlayerController>()?.SetMouseSensitivity(v)`.
- [ ] İpucu paneli ([H]) duraklatılmışken açılmasın.

### 3.3 Kabul ölçütleri
- [ ] Esc → menü açılır, oyun durur (yoldaş, köprü, parça dönmesi durmuş), fare serbest.
- [ ] Devam Et → kaldığı yerden devam, fare kilitli.
- [ ] Muhafızla konuşurken Esc → **sadece diyalog kapanır**, duraklatma menüsü açılmaz.
- [ ] Ana Menü → menü açılır; Başla → oyun normal hızda başlar.

- [ ] **PR aç:** `omer/duraklatma` → `main`.

---

## Adım 4 — README ve Rapor (6.–7. gün)

Dal: `omer/readme`

### 4.1 Ekran görüntüleri (`Docs/Images/`, PNG — otomatik LFS)
- [ ] 1920×1080 Game penceresi; `Win + Shift + S`:
  1. Ana menü
  2. Bölüm 1: duvarlarla çevrili alan, korkuluklu uçurum kenarı, kalkık köprü
  3. Yoldaş platforma zıplarken / şalteri veya parçayı alırken
  4. Parça toplama anı + "Yankı Taşı: 3/5" sayacı
  5. 5/5 → ipucu paneli
  6. Muhafızla sohbet + "düşünüyor..." göstergesi
  7. Kapı açılırken
  8. Zafer ekranı
  9. Ollama kapalıyken hata mesajı (çökmediğini gösterir)
  10. Test Runner — tüm testler yeşil (Erdem'den iste)

### 4.2 `README.md`'yi son hâline getir
- [ ] **Başlık + kapak görseli**
- [ ] **Oyun hakkında:** hikâye (Yankı Tapınağı, muhafız Kaan, yoldaş Robo, Yankı Taşı), amaç
- [ ] **Nasıl oynanır:** kontroller (WASD, Shift, Space, E, F, **H**, Esc), bölümler (çözümü verme, ipucu ver)
- [ ] **Kurulum:** kısa özet + "ayrıntılar için [KURULUM.md](KURULUM.md)"
- [ ] **Teknik özellikler:** NavMesh yoldaş, sensörler, Ollama JSON şeması, hata yönetimi, **toplama görevi (ICollectionQuest, olay tabanlı)**
- [ ] **Zorluk mekanikleri / Mimari / LLM güvenliği:** Erdem'in gönderdiği metinler
- [ ] **Ekran görüntüleri** galerisi
- [ ] **Görev dağılımı:** kim ne yaptı (YAPILACAKLAR'daki tablo)
- [ ] **Kullanılan asset'ler ve lisanslar:** `Assets/Art/LISANSLAR.txt`'den
- [ ] Mevcut README'deki ayrıntılı sahne kurulum adımlarını → `Docs/SAHNE_KURULUMU.md`'ye taşı, link ver.

### 4.3 Rapor için
- [ ] Asset'ler, lisanslar, kaynak linkleri
- [ ] Görsel tasarım kararları (tema, renk paleti, parça parıltısı)
- [ ] Menü akış diyagramı: MainMenu → Main → Pause/Victory → MainMenu
- [ ] Toplama görevi akış diyagramı: Parça [E]/[F] → CollectionQuest → HUD sayacı → 5/5 → ipucu → GameManager kapı kuralı
- [ ] Karşılaşılan sorunlar ve çözümleri (Esc çakışması, timeScale, Türkçe karakter, trigger collider + raycast)
- [ ] (İsteğe bağlı) 1–2 dk oynanış videosu (Xbox Game Bar `Win + G`) — **repoya koyma**, link ver.

- [ ] **PR aç:** `omer/readme` → `main`.

---

## Erdem'le Alışveriş Listesi

| Ne | Kimden → Kime | Ne zaman |
|---|---|---|
| `SetPaused` API, `MouseSensitivity`, `SetMouseSensitivity` | Erdem → Ömer | 1. gün (kod hazır, test/PR aşamasında) |
| **`ICollectionQuest` arayüzü** | Erdem → Ömer | **1. gün** |
| Skybox materyali `TempleSky.mat` | Ömer → Erdem | ✅ main'de, sahneye Erdem atayacak |
| **`Wall.mat`** | Ömer → Erdem | 1. gün |
| **`StoneRailing` / `BridgeRailing` prefab'ları** | Ömer → Erdem | 2. gün |
| Ana menü sahnesi + Build Settings | Ömer | 1. gün |
| **`EchoShard` prefab + `Collectible` + `CollectionQuest` + `QuestHintData` + HUD sayaç/ipucu** | Ömer → Erdem | 3. gün |
| Parçaları sahneye yerleştirme, kapıya bağlama, testler | Erdem | 4. gün |
| PauseMenu prefab'ı, (modeller) | Ömer → Erdem | 4.–5. gün |
| Zafer paneli butonu objesi (kod sende hazır) | Erdem sahneye ekler | 5. gün |
| Zorluk / Mimari / Güvenlik metinleri, test ekran görüntüsü | Erdem → Ömer | 6. gün |
| Birleştirme, tam oyun testi, teslim | Birlikte | 7. gün |

## Son Kontrol Listesi (teslimden önce)
- [ ] Temiz klasöre clone → texture'lar görünüyor (LFS çalışıyor).
- [ ] MainMenu → Başla → 5 parça → ipucu → muhafız → zafer → Ana Menü → Başla → oyun normal hızda.
- [ ] Türkçe karakterler tüm ekranlarda düzgün.
- [ ] `Assets/Art/LISANSLAR.txt` eksiksiz (texture + ses + model), README'de lisans bölümü var.
- [ ] README'deki tüm linkler ve resimler GitHub sayfasında açılıyor.
