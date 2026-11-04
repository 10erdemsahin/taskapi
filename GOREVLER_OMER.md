# Ömer'in Görevleri — Görsel, Menü, Doküman

Önce [KURULUM.md](KURULUM.md)'yi baştan sona uygula. Genel plan: [YAPILACAKLAR.md](YAPILACAKLAR.md).
Erdem'in görevleri: [GOREVLER_ERDEM.md](GOREVLER_ERDEM.md).

**Repo:** https://github.com/10erdemsahin/taskapi (Erdem'in davetini kabul etmeyi unutma)

## Senin Alanın (sadece bunlara dokunursun)

| Sahibi olduğun | Dokunmayacağın (Erdem'in) |
|---|---|
| `Assets/Art/**` (texture, skybox, prefab, model, ses) | `Assets/Scenes/Main.unity` (açıp oynatabilirsin, **kaydetmezsin**) |
| `Assets/Materials/**` (mevcut `.mat` dosyaları) | `Assets/Scripts/**` (**`Scripts/UI/` hariç**) |
| `Assets/Scripts/UI/**` (DialogueUI, GameHUD, yeni menüler) | `Assets/Editor/**`, `Assets/Tests/**`, `Assets/Data/**` |
| `Assets/Scenes/MainMenu.unity` (yeni) | `ProjectSettings/` (**Build Settings hariç**), `Packages/` |
| `ProjectSettings/EditorBuildSettings.asset` (sahne sırası) | |
| `README.md`, `Docs/**` (ekran görüntüleri) | |

> **Main.unity'yi yanlışlıkla kaydettiysen** (Git'te değişmiş görünüyorsa) commit'lemeden geri al:
> `git checkout -- Assets/Scenes/Main.unity`
> Sahnede bir değişiklik gerekiyorsa (materyal atama, buton ekleme, prefab yerleştirme) **Erdem'e yaz**, o yapar.

**Bozulmayacak kurallar:**
- `DialogueUI`'nin "düşünüyor..." göstergesi ve LLM beklerken girişin kilitlenmesi **kaldırılmaz** (ders isteri).
- `DialogueUI` yalnızca gösterir; NPC/LLM mantığı UI'ye taşınmaz.
- Menüden sahne yüklemeden önce **`Time.timeScale = 1f`** yapılır (yoksa oyun donuk başlar).

---

## Adım 0 — Hazırlık (1. gün)

- [ ] [KURULUM.md](KURULUM.md) Adım 1–8 tamam: Unity **6000.6.7f1**, Git, **Git LFS**, Ollama, clone (`C:\Projeler\taskapi`), Smart Merge ayarı.
- [ ] `Main.unity`'yi aç → Play → oyunu baştan sona bir kez oyna (F ile yoldaş, E ile muhafız). Oyunu tanı.
- [ ] Kendi dalını aç:
```bash
git checkout -b omer/texture
```
- [ ] Erdem'in **`SetPaused` API'si** main'e girene kadar duraklatma menüsüne başlama (texture ve ana menü ile başla).

---

## Adım 1 — Texture Pack (1.–3. gün)

Dal: `omer/texture`

### 1.1 Paket seç ve indir
- [ ] Lisansı **CC0** olan (atıf zorunlu değil, ticari dahil serbest) kaynaklardan seç:
  - **ambientCG.com** → Ground (çimen/toprak), Rock/Bricks (tapınak taşı), Wood (köprü), Metal (şalter)
  - **polyhaven.com** → aynı kategoriler + HDRI gökyüzü
  - **kenney.nl** → stilize / low-poly istersen
- [ ] Tema: **antik tapınak / orman** (taş, yosun, eski ahşap). Hepsi aynı tarzda olsun (biri gerçekçi biri çizgi film olmasın).
- [ ] **Boyut: 1K veya 2K, JPG.** 4K/8K indirme (GitHub LFS ücretsiz kota 1 GB, her clone bunu harcar).
- [ ] ambientCG zip'inden sadece şunları al: `_Color` (renk), `_NormalGL` (normal — **GL** olanı, DX değil), varsa `_AmbientOcclusion`. Diğerlerini (Displacement, Roughness...) projeye koyma.

### 1.2 Projeye ekle
- [ ] Klasör yapısı (Unity'nin Project penceresinden oluştur):
```
Assets/Art/
├── Textures/Ground/   Ground_Color.jpg, Ground_NormalGL.jpg
├── Textures/Stone/
├── Textures/Wood/
├── Textures/Metal/
├── Skybox/
├── Prefabs/
└── LISANSLAR.txt
```
- [ ] Her `_NormalGL` dosyasını seç → Inspector → **Texture Type: Normal map** → Apply.
- [ ] Tüm texture'larda **Max Size: 2048**.
- [ ] `Assets/Art/LISANSLAR.txt`: her paket için *ad, site, link, lisans (CC0)*. Raporda kullanılacak.

### 1.3 Materyallere uygula (`Assets/Materials/*.mat`)
Shader **Standard** (Built-in render pipeline). Her materyalde:
- [ ] **Albedo**: `_Color` texture'ı sürükle, yanındaki renk kutusunu **BEYAZ** yap (yoksa texture renklenir).
- [ ] **Normal Map**: `_NormalGL`, şiddet 1.
- [ ] **Occlusion**: varsa `_AmbientOcclusion`.
- [ ] **Smoothness**: taş/zemin 0.1–0.2, metal 0.5–0.6.
- [ ] **Tiling** (önerilen başlangıç, oyunda bakıp ayarla):

| Materyal | Nerede kullanılıyor | Texture | Tiling |
|---|---|---|---|
| `Ground.mat` | İki zemin (20×20 m) | Çimen/toprak | (10, 10) |
| `Stone.mat` | Platform, duvarlar, şalter tabanı | Tapınak taşı | (2, 2) |
| `Wood.mat` | Köprü güvertesi (3×7 m) | Ahşap tahta | (1, 3) |
| `Door.mat` | Kapı paneli | Eski taş/metal kapı | (1, 1) |
| `Lever.mat` | Şalter kolu | Paslı metal | (1, 1) |
| `Player.mat`, `Companion.mat`, `Guard.mat` | Kapsüller | (renk kalsın veya kumaş) | — |

- [ ] **Yeni materyal gerekirse** (ör. duvar için ayrı taş): `Assets/Materials/` içinde oluştur, **Erdem'e** "şu objeye şu materyali ata" diye yaz (atama sahneye kaydedilir → Erdem'in işi).
- [ ] **Kontrol:** Git'te sadece `Assets/Art/**` ve `Assets/Materials/**` değişmiş olmalı, `Main.unity` **değişmemiş** olmalı:
```bash
git status
```

### 1.4 Skybox
- [ ] Kolay yol: *Assets → Create → Material* → `Assets/Art/Skybox/TempleSky.mat` → Shader: **Skybox/Procedural** → güneş boyutu, atmosfer kalınlığı, renk ayarla.
- [ ] Güzel yol: Poly Haven'dan **1K/2K `.hdr`** HDRI indir → texture'ı seç → *Texture Shape: Cube* → Apply → materyal Shader: **Skybox/Cubemap** → HDRI'yi ata.
- [ ] Denemek için *Window → Rendering → Lighting → Environment → Skybox Material* → kendi materyalin → Play'de bak → **sahneyi KAYDETME**, Erdem'e "skybox hazır: `Assets/Art/Skybox/TempleSky.mat`" yaz.

### 1.5 (İsteğe bağlı) Karakter modelleri
- [ ] CC0 düşük poligon karakterler (Kenney → "Mini Characters" / "Blocky Characters") → `Assets/Art/Models/`.
- [ ] Her biri için prefab: `Assets/Art/Prefabs/PlayerModel.prefab`, `CompanionModel.prefab`, `GuardModel.prefab`. Prefab **sadece görsel** olsun: collider, script, rigidbody **yok**. Boyu ~2 m, ayakları y = -1'de (kapsülün merkezine göre).
- [ ] Erdem'e ver; o kapsüllerin altına yerleştirir.

- [ ] **PR aç:** `omer/texture` → `main`. Erdem onaylayınca merge.

---

## Adım 2 — Ana Menü (2.–4. gün)

Dal: `omer/ana-menu` (önce `git checkout main` + `git pull`)

### 2.1 Sahne
- [ ] *File → New Scene → Basic (Built-in)* → `Assets/Scenes/MainMenu.unity` olarak kaydet.
- [ ] *GameObject → UI → Canvas* → Canvas Scaler: **Scale With Screen Size**, 1920×1080, Match 0.5.
- [ ] EventSystem objesinde **Standalone Input Module** olmalı (proje eski Input Manager kullanıyor; "Input System UI Input Module"e **çevirme**).
- [ ] Hiyerarşi:
```
Canvas
├── Background        (Image / oyundan ekran görüntüsü, hafif karartılmış)
├── Title             (TMP: "Yankı Tapınağı")
├── MainPanel
│   ├── StartButton   "Başla"
│   ├── HowToButton   "Nasıl Oynanır"
│   ├── SettingsButton "Ayarlar"
│   └── QuitButton    "Çıkış"
├── HowToPanel        (kapalı başlar) → kontroller + kısa hikâye + "Geri"
└── SettingsPanel     (kapalı başlar)
    ├── SensitivitySlider (0.5 – 5, varsayılan 2) + değer yazısı
    ├── VolumeSlider      (0 – 1, varsayılan 1)  + değer yazısı
    └── BackButton "Geri"
```
- [ ] Tüm yazılar **TextMeshPro**. Türkçe karakterler (ı, ş, ğ, ü, ö, ç, İ) düzgün görünüyor mu kontrol et. Farklı font eklersen: fontu seç → *Window → TextMeshPro → Font Asset Creator* yerine sağ tık → *Create → TextMeshPro → Font Asset* → **Atlas Population Mode: Dynamic**.

### 2.2 Script — `Assets/Scripts/UI/Menu/MainMenuUI.cs`
- [ ] Namespace `AdventureGame` (diğer dosyalar gibi). İskelet:
```csharp
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AdventureGame
{
    public sealed class MainMenuUI : MonoBehaviour
    {
        public const string SensitivityKey = "MouseSensitivity"; // Erdem'in PlayerController'ı bu anahtarı okuyor
        public const string VolumeKey = "MasterVolume";
        private const string GameSceneName = "Main";

        [SerializeField] private GameObject mainPanel, howToPanel, settingsPanel;
        [SerializeField] private Slider sensitivitySlider, volumeSlider;
        [SerializeField] private TMP_Text sensitivityValue, volumeValue;

        private void Awake()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            // Slider'ları PlayerPrefs'ten doldur, AudioListener.volume'u uygula, paneller: sadece mainPanel açık
        }

        public void StartGame() { Time.timeScale = 1f; SceneManager.LoadScene(GameSceneName); }
        public void ShowPanel(GameObject panel) { /* diğerlerini kapat, bunu aç */ }
        public void OnSensitivityChanged(float v) { PlayerPrefs.SetFloat(SensitivityKey, v); /* yazıyı güncelle */ }
        public void OnVolumeChanged(float v) { PlayerPrefs.SetFloat(VolumeKey, v); AudioListener.volume = v; }
        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
```
- [ ] Butonların **OnClick**'ini Inspector'dan bağla (Slider → **On Value Changed** → *Dynamic float* olanı seç).
- [ ] Ayar değişince `PlayerPrefs.Save()` çağır (panel kapanırken).

### 2.3 Build Settings (sahne sırası senin sorumluluğunda)
- [ ] *File → Build Profiles* (veya *Build Settings*) → Scene List:
  - **0:** `Assets/Scenes/MainMenu.unity`
  - **1:** `Assets/Scenes/Main.unity`
- [ ] Commit'te `ProjectSettings/EditorBuildSettings.asset` değişecek, normal. PR açıklamasına "sahne sırası değişti" yaz.

### 2.4 Kabul ölçütleri
- [ ] Menü → Başla → oyun açılıyor, fare kilitleniyor.
- [ ] Ayarlardan hassasiyet değişince oyunda fare hızı değişiyor (Erdem'in `PlayerPrefs` satırı main'e girdikten sonra).
- [ ] Ses ayarı kalıcı (oyunu kapatıp açınca hatırlıyor).
- [ ] Console'da hata yok.

- [ ] **PR aç:** `omer/ana-menu` → `main`.

---

## Adım 3 — Duraklatma Menüsü (4.–5. gün)

Dal: `omer/duraklatma`. **Önkoşul:** Erdem'in `GameManager.SetPaused` / `IsPaused` / `PausedChanged` API'si main'de olmalı.

### 3.1 Prefab
- [ ] `Main.unity`'yi aç → Canvas'ın altına `PauseMenu` paneli kur: yarı saydam siyah arka plan + "Duraklatıldı" + **Devam Et / Ayarlar / Ana Menü / Çıkış**. Ayarlar paneli ana menüdekiyle aynı slider'lar.
- [ ] Paneli `Assets/Art/Prefabs/PauseMenu.prefab` olarak sürükleyip prefab yap → sahneden **sil** → **sahneyi kaydetme** (gerekirse `git checkout -- Assets/Scenes/Main.unity`).
- [ ] Erdem'e: "PauseMenu prefab hazır, Canvas altına koyar mısın?"

### 3.2 Script — `Assets/Scripts/UI/Menu/PauseMenuUI.cs` (prefab'ın kök objesinde)
- [ ] **Esc** ile aç/kapat → `GameManager.Instance.SetPaused(true/false)`.
- [ ] `GameManager.Instance.PausedChanged`'e abone ol → paneli göster/gizle (oyun başka yerden de duraklatılırsa UI tutarlı kalsın). `OnDestroy`'da aboneliği bırak.
- [ ] **ÇOK ÖNEMLİ — Esc çakışması:** Esc tuşu diyalog penceresini de kapatıyor (`DialogueUI`). Diyalog kapanırken aynı karede duraklatma menüsü **açılmamalı**:
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
- [ ] `Time.timeScale = 0` iken çalışacak her animasyon/bekleme **unscaled** olmalı (`WaitForSecondsRealtime`, `Time.unscaledDeltaTime`).
- [ ] **Ana Menü** butonu: `gm.SetPaused(false); Time.timeScale = 1f; SceneManager.LoadScene("MainMenu");`
- [ ] Ayarlar: `MainMenuUI.SensitivityKey` / `VolumeKey` sabitlerini kullan (anahtar adlarını tekrar yazma). Hassasiyet oyun içinde **anında** uygulanacaksa Erdem'den `PlayerController`'a `public void SetMouseSensitivity(float v)` iste.
- [ ] Test için prefab'ı geçici olarak sahneye koyup Play'e bas; bitince sahneyi **kaydetmeden** kapat.

### 3.3 Zafer paneline "Ana Menü" butonu — `Assets/Scripts/UI/GameHUD.cs`
- [ ] `GameHUD`'a alan ekle: `[SerializeField] private Button mainMenuButton;` → `Awake`'te `onClick` → `Time.timeScale = 1f; SceneManager.LoadScene("MainMenu");`
- [ ] Buton objesi sahnede (VictoryPanel altında) olacağı için: **Erdem'e** "VictoryPanel'e `MainMenuButton` ekleyip GameHUD'daki `mainMenuButton` alanına bağlar mısın, SceneBuilder'a da ekle" diye yaz.

### 3.4 Kabul ölçütleri
- [ ] Oyunda Esc → menü açılır, oyun durur (yoldaş, köprü, kapı animasyonu durmuş), fare serbest.
- [ ] Devam Et → oyun kaldığı yerden devam, fare kilitli.
- [ ] Muhafızla konuşurken Esc → **sadece diyalog kapanır**, duraklatma menüsü açılmaz.
- [ ] Ana Menü → menü sahnesi açılır, sonra Başla → oyun normal hızda başlar.
- [ ] Zafer ekranında Ana Menü butonu çalışır.

- [ ] **PR aç:** `omer/duraklatma` → `main`.

---

## Adım 4 — README ve Rapor (6.–7. gün)

Dal: `omer/readme`

### 4.1 Ekran görüntüleri
- [ ] Game penceresini **1920×1080** yap, oyunu oynayıp şu anları yakala (Windows: `Win + Shift + S`):
  1. Ana menü
  2. Bölüm 1: uçurum + kalkık köprü
  3. Yoldaş platforma zıplarken / şalteri çekerken (Scene görünümünde yeşil Raycast çizgileri de güzel durur)
  4. Muhafızla sohbet + "düşünüyor..." göstergesi
  5. Kapı açılırken
  6. Zafer ekranı
  7. Ollama kapalıyken hata mesajı (çökmediğini gösterir)
  8. Test Runner — tüm testler yeşil (Erdem'den iste)
- [ ] `Docs/Images/01_menu.png` ... şeklinde kaydet (PNG'ler otomatik LFS'e gider).

### 4.2 `README.md`'yi son hâline getir
Şu anki README teknik kurulum rehberi. Teslim için şu sırayla yeniden düzenle:
- [ ] **Başlık + kapak görseli** (ana menü veya oyun içi)
- [ ] **Oyun hakkında:** hikâye (Yankı Tapınağı, muhafız Kaan, yoldaş Robo), amaç
- [ ] **Nasıl oynanır:** kontroller tablosu, bölümlerin anlatımı (çözümü verme, ipucu ver)
- [ ] **Kurulum:** kısa özet + "ayrıntılar için [KURULUM.md](KURULUM.md)"
- [ ] **Teknik özellikler:** NavMesh yoldaş, sensörler, Ollama JSON şeması, hata yönetimi (mevcut README'den kısalt)
- [ ] **Zorluk mekanikleri / Mimari / LLM güvenliği:** Erdem'in gönderdiği metinler
- [ ] **Ekran görüntüleri** galerisi
- [ ] **Görev dağılımı:** kim ne yaptı (YAPILACAKLAR'daki tablo)
- [ ] **Kullanılan asset'ler ve lisanslar:** `Assets/Art/LISANSLAR.txt`'den
- [ ] Mevcut README'deki ayrıntılı sahne kurulum adımları (koordinatlar vb.) → `Docs/SAHNE_KURULUMU.md`'ye taşı, README'den link ver.

### 4.3 Rapor için (ders dökümanının şablonuna göre)
- [ ] Kullanılan asset'ler, lisansları ve kaynak linkleri
- [ ] Görsel tasarım kararları (tema, renk paleti)
- [ ] Menü akış diyagramı: MainMenu → Main → Pause/Victory → MainMenu
- [ ] Karşılaşılan sorunlar ve çözümleri (ör. Esc çakışması, timeScale, Türkçe karakter)
- [ ] (İsteğe bağlı) 1–2 dk oynanış videosu: *Window → General → Recorder* (paket gerekir, Erdem'e sor) veya Xbox Game Bar (`Win + G`). Videoyu **repoya koyma**, YouTube/Drive linki ver.

- [ ] **PR aç:** `omer/readme` → `main`.

---

## Erdem'le Alışveriş Listesi

| Ne | Kimden → Kime | Ne zaman |
|---|---|---|
| `SetPaused` API, `MouseSensitivity` okuma satırı | Erdem → Ömer | 1. gün |
| Skybox materyali | Ömer → Erdem (sahneye atar) | 2.–3. gün |
| Yeni materyal atamaları (varsa) | Ömer → Erdem | 3. gün |
| Karakter model prefab'ları (isteğe bağlı) | Ömer → Erdem | 3.–4. gün |
| PauseMenu prefab'ı | Ömer → Erdem (Canvas altına koyar) | 5. gün |
| Zafer paneline Ana Menü butonu (sahne kısmı) | Ömer kodu yazar → Erdem butonu ekler | 5. gün |
| Zorluk / Mimari / Güvenlik metinleri, test ekran görüntüsü | Erdem → Ömer | 6. gün |
| Birleştirme, tam oyun testi, teslim | Birlikte | 7. gün |

## Son Kontrol Listesi (teslimden önce)
- [ ] Temiz klasöre clone → texture'lar görünüyor (LFS çalışıyor).
- [ ] MainMenu → Başla → oyun → Esc → Ana Menü → Başla → oyun normal hızda.
- [ ] Türkçe karakterler tüm ekranlarda düzgün.
- [ ] `Assets/Art/LISANSLAR.txt` eksiksiz, README'de lisans bölümü var.
- [ ] README'deki tüm linkler ve resimler GitHub sayfasında açılıyor.
