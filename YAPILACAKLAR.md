# Yapılacaklar — Görev Dağılımı (Ömer & Erdem)

Projenin şu anki durumu: Oynanabilir prototip hazır (Bölüm 1: NavMesh yoldaş + köprü, Bölüm 2: Ollama NPC + kapı).
Kalan işler: **README / rapor**, **texture pack**, **menü**, **görevleri zorlaştırma**.

İşler, iki kişi **aynı dosyaya aynı anda dokunmayacak** şekilde bölündü:

| | **Ömer** — Görsel, Menü, Doküman | **Erdem** — Oynanış, Zorluk, Test |
|---|---|---|
| Ana iş | Texture pack + Ana menü + Duraklatma menüsü + README/rapor | Bölüm 1 ve Bölüm 2'yi zorlaştırma + testler |
| Sahibi olduğu klasörler | `Assets/Art/`, `Assets/Materials/`, `Assets/Scripts/UI/`, `Assets/Scenes/MainMenu.unity`, `README.md` | `Assets/Scripts/` (UI hariç), `Assets/Editor/`, `Assets/Tests/`, `Assets/Data/`, `Assets/Scenes/Main.unity` |
| Dokunmaması gereken | `Main.unity`, `GameManager.cs`, `SceneBuilder.cs` | `Scripts/UI/`, `Materials/`, `MainMenu.unity` |

> **Neden böyle?** Unity sahne dosyaları (`.unity`) birleştirilmesi (merge) en zor dosyalardır.
> `Main.unity`'yi yalnızca Erdem değiştirir. Ömer'in texture'ları **materyal dosyalarının içine** girer
> (`Assets/Materials/*.mat`); sahne materyallere GUID ile bağlı olduğu için sahne dosyası değişmez ama oyunda texture'lar görünür.

---

## 0. Ortak Başlangıç (ilk gün, birlikte — ~1 saat)

- [ ] **Git kurulumu** (Erdem): GitHub'da private repo aç, Unity `.gitignore` ekle (`Library/`, `Temp/`, `Logs/`, `UserSettings/`, `*.csproj`, `*.sln` hariç tutulsun), ilk commit'i at, Ömer'i ekle.
- [ ] **Unity ayarları** (Erdem): *Edit → Project Settings → Editor* → Asset Serialization: **Force Text**, Version Control: **Visible Meta Files**.
- [ ] **Branch düzeni:** herkes kendi dalında çalışır → `omer/gorsel-menu`, `erdem/oynanis`. `main`'e sadece çalışan hâli birleştirin (Pull Request).
- [ ] **GameManager'a duraklatma API'si** (Erdem, ilk gün): Ömer'in menüsü buna ihtiyaç duyuyor, bu yüzden en önce bu yapılır:
  ```csharp
  public bool IsPaused { get; private set; }
  public event Action<bool> PausedChanged;
  public void SetPaused(bool paused) { /* Time.timeScale, SetInputLocked, PausedChanged */ }
  ```
- [ ] İkisi de projeyi kendi bilgisayarında açıp `Main.unity`'yi oynatabildiğini doğrular.

---

## 1. ÖMER — Görsel, Menü, Doküman

### 1.1 Texture Pack
- [ ] Lisansı **CC0** (ücretsiz, atıf zorunlu değil) bir paket seç. Önerilen kaynaklar:
  - **ambientCG.com** ve **polyhaven.com** → taş, zemin, ahşap, metal PBR texture'ları
  - **kenney.nl** → düşük poligon model/texture paketleri
- [ ] İndirilenleri `Assets/Art/Textures/<PaketAdı>/` altına koy. Kaynak ve lisansı `Assets/Art/LISANSLAR.txt`'ye yaz (raporda gerekecek).
- [ ] Mevcut materyallere texture ata (Inspector → Albedo, Normal Map, Tiling):
  | Materyal | Önerilen | Not |
  |---|---|---|
  | `Ground.mat` | Çimen / toprak | Tiling ~ (10, 10) — zemin 20×20 m |
  | `Stone.mat` | Tapınak taşı / tuğla | Platform, duvarlar |
  | `Wood.mat` | Ahşap tahta | Köprü |
  | `Door.mat` | Eski taş kapı / metal | |
  | `Lever.mat` | Paslı metal | |
- [ ] Skybox ekle (*Window → Rendering → Lighting → Environment → Skybox Material*) — bu ayar sahnede saklandığı için **materyali Ömer hazırlar, sahneye Erdem atar.**
- [ ] (İsteğe bağlı) Kapsül yerine karakter modelleri: modeli prefab yap, Erdem'e ver; Erdem sahnede kapsülün altına ekler.
- [ ] **Kabul ölçütü:** Oyunda texture'lar doğru ölçekte görünüyor, FPS düşmüyor, Git'te `Main.unity` değişmemiş.

### 1.2 Ana Menü
- [ ] Yeni sahne: `Assets/Scenes/MainMenu.unity`.
- [ ] Yeni script: `Assets/Scripts/UI/Menu/MainMenuUI.cs`
  - **Başla** → `SceneManager.LoadScene("Main")`
  - **Nasıl Oynanır** → kontroller paneli (WASD, Shift, Space, E, F, Esc) + kısa hikâye
  - **Ayarlar** → fare hassasiyeti, ses seviyesi (değerler `PlayerPrefs`'e kaydedilir)
  - **Çıkış** → `Application.Quit()`
- [ ] Arka plan: oyundan bir ekran görüntüsü veya yavaş dönen kamera.
- [ ] *File → Build Settings*: `MainMenu` **0. sırada**, `Main` 1. sırada.
- [ ] Fare hassasiyeti ayarının oyunda okunması için Erdem'den `PlayerController`'a şu satırı istemek:
  `mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", mouseSensitivity);`

### 1.3 Duraklatma Menüsü (oyun içi, Esc)
- [ ] `Assets/Scripts/UI/Menu/PauseMenuUI.cs` + bir **prefab** (`Assets/Art/Prefabs/PauseMenu.prefab`): Devam Et / Ayarlar / Ana Menü / Çıkış.
- [ ] **Dikkat:** Esc tuşu şu an diyalog penceresini de kapatıyor. Diyalog açıkken duraklatma menüsü açılmamalı:
  `if (GameManager.Instance.IsInputLocked && !GameManager.Instance.IsPaused) return;`
- [ ] Açılınca `GameManager.Instance.SetPaused(true)` çağrılır (Erdem'in 0. adımda eklediği API).
- [ ] Prefab hazır olunca **Erdem sahneye ekler** (sahne Erdem'de).
- [ ] `GameHUD.cs` zafer paneline **"Ana Menü"** butonu ekle (`GameHUD` UI klasöründe → Ömer'in).

### 1.4 README ve Rapor
- [ ] `README.md`'yi son hâle getir: oyun tanıtımı, ekran görüntüleri (menü, Bölüm 1, Bölüm 2, zafer), kurulum, kontroller.
- [ ] Erdem'den gelen "Zorluk Mekanikleri" ve "Mimari" bölümlerini README'ye yerleştir.
- [ ] Rapor için: kullanılan asset'ler ve lisansları, takım içi görev dağılımı (bu dosya), karşılaşılan sorunlar.
- [ ] (İsteğe bağlı) 1–2 dakikalık oynanış videosu.

---

## 2. ERDEM — Oynanışı Zorlaştırma ve Test

> Kural: Yeni zorluklar da **ders isterlerine uygun** kalmalı. Yoldaşın hareketi NavMesh ile olmalı
> (sabit koordinat yok), NPC kararı LLM'den JSON şemasıyla gelmeli.

### 2.1 Bölüm 1 — Fiziksel Engeli Zorlaştır (en az 2 tanesi)
- [ ] **Çift kilit (işbirliği):** Köprü iki koşulla iner. Biri yüksek platformdaki şalter (yoldaş), diğeri bir **basınç plakası** (oyuncu üstünde durmalı).
  - Yeni: `PressurePlate.cs` (oyuncu üstündeyken aktif), `AndGate.cs : IActivatable` (iki girdi de aktifse köprüyü çalıştırır).
  - `LeverSwitch` zaten `IActivatable`'a bağlı → şalterin hedefi `AndGate` yapılır, kod değişmez.
- [ ] **Hareketli engel:** Yoldaşın yolunu periyodik kesen sürgülü taş blok (`NavMeshObstacle`, Carve açık). Yoldaş yeniden rota hesaplamalı veya beklemeli.
- [ ] **Gizli şalter:** Şalter başlangıç noktasından görünmesin (önüne bir sütun koy). Oyuncu yoldaşı doğru noktaya götürmeli; `CompanionSensor` görüş kontrolü zaten bunu destekliyor. İsteğe bağlı: hiçbir şey görmezse ara noktalar (waypoint) arasında keşfe çıkma.

### 2.2 Bölüm 2 — Sosyal Engeli Zorlaştır (en az 2 tanesi)
- [ ] **Kanıt eşyası:** Yoldaş Bölüm 1'de platformdan bir **"Tapınak Mührü"** alır. Muhafız mühür olmadan kapıyı açmaz.
  - Bu kural **Unity tarafında** zorlanır: `GameManager.HandleNpcActionRequested` içinde `if (!HasSeal) reddet`. Böylece oyuncu LLM'i kandırsa bile kapı açılmaz.
  - Persona'ya şunu ekle: "Yolcu mührü göstermeden kapıyı açma."
- [ ] **İki aşamalı bilmece:** JSON şemasına `"stage": 1|2` veya `"riddle_solved": true/false` alanı ekle. Kapı ancak ikinci bilmeceden sonra açılır.
  - Değişecek dosyalar: `NpcResponseSchema`, `NpcLLMResponseDto`, `NpcDecision`, `NpcDecisionParser`, persona metni.
- [ ] **Sınırlı deneme:** 3 yanlış cevaptan sonra muhafız 30 saniye konuşmaz (bekleme süresi). Kalan hak sayısı sohbet penceresinde sistem mesajı olarak gösterilir.
- [ ] **Güven puanı (isteğe bağlı):** Şemaya `"trust": 0-100` ekle. Kaba konuşma güveni düşürür, kibarlık artırır; güven 60'ın altındaysa kapı açılmaz.

### 2.3 GameManager ve Sahne
- [ ] 0. adımdaki `SetPaused` API'si (ilk gün!).
- [ ] Yeni aşamalara göre görev metinlerini güncelle.
- [ ] `PlayerController`'a `PlayerPrefs` fare hassasiyeti satırını ekle (Ömer'in ayarlar menüsü için).
- [ ] Ömer'in skybox'ını, PauseMenu prefab'ını ve modellerini `Main.unity`'ye yerleştir.
- [ ] Yeni objeleri `SceneBuilder.cs`'e de ekle ki *Tools → AdventureGame → 2) Sahneyi Oluştur* her şeyi baştan kurabilsin.
  > Not: SceneBuilder materyalleri yeniden oluştururken Ömer'in texture'larını silmemeli. `Mat()` metodu mevcut `.mat` dosyasını
  > yüklüyor, sadece rengini değiştiriyor. Texture atanmış materyallerde renk satırı atlanmalı.

### 2.4 Testler
- [ ] Her yeni mekanik için PlayMode testi (`Assets/Tests/PlayMode/`):
  - Basınç plakası boşken şalter çekilirse köprü inmemeli
  - Mühür yokken `open_door` gelirse kapı açılmamalı
  - 3 yanlıştan sonra mesaj gönderilemiyor olmalı
- [ ] Mevcut testleri çalıştır (*Window → General → Test Runner → PlayMode → Run All*). Not: bu testler henüz hiç çalıştırılmadı; ilk çalıştırmada çıkan hataları düzelt.
- [ ] README için Ömer'e **"Zorluk Mekanikleri"** ve **"Mimari (sınıf diyagramı)"** bölümlerinin metnini ver.

---

## 3. Birleştirme ve Teslim (son gün, birlikte)

- [ ] İki dalı `main`'e birleştir, çakışmaları çöz.
- [ ] Tam oyun akışını baştan sona oyna: Menü → Bölüm 1 → Bölüm 2 → Zafer → Ana Menü.
- [ ] Hata senaryoları: Ollama kapalıyken oyun çökmüyor, NPC "düşünüyor..." gösteriyor.
- [ ] Tüm testler yeşil.
- [ ] README ve rapor son kontrol, ekran görüntüleri güncel.

---

## Özet Takvim (öneri)

| Gün | Ömer | Erdem |
|---|---|---|
| 1 | Git'e katıl, texture paketini seç ve indir | Git kur, `SetPaused` API'si, mevcut testleri çalıştır |
| 2–3 | Materyaller + skybox + Ana Menü | Bölüm 1 zorlukları (çift kilit, hareketli engel) |
| 4–5 | Duraklatma menüsü, zafer paneli butonu | Bölüm 2 zorlukları (mühür, iki aşama, deneme hakkı) |
| 6 | README taslağı, ekran görüntüleri | Testler, SceneBuilder güncellemesi, mimari metni |
| 7 | **Birlikte:** birleştirme, tam test, teslim | |
