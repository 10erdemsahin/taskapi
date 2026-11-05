# Erdem — Teslime Kadar Yapılacaklar

**Son teslim: 6 Kasım 2026 Cuma 17:00.** Teslim paketi tek bir zip: proje kodları + **build edilmiş oyun** + GitHub'daki README raporu.
Sıra öncelik sırasıdır. Vakit yetmezse 🔵 işaretli maddeler atlanabilir; 🔴 ve 🟠 işaretliler zorunlu.

---

## 🔴 1. PR'ları incele ve merge et (≈15 dk)

| PR | İçerik | Kontrol |
|---|---|---|
| [#2 Ana menü](https://github.com/10erdemsahin/taskapi/pull/2) | `MainMenuUI`, `GameSettings`, `MainMenu.unity`, Build Settings (0: MainMenu, 1: Main), `GameHUD.mainMenuButton` alanı | Dalı çek → `MainMenu.unity` → Play → Başla → oyun açılıyor mu? |
| [#3 README + testler](https://github.com/10erdemsahin/taskapi/pull/3) | README rapor olarak yeniden yazıldı (Mermaid diyagramlar), `Docs/SAHNE_KURULUMU.md`, `Docs/Tests/` (PlayMode + gerçek Ollama sonuçları) | GitHub'da README'nin diyagramları düzgün görünüyor mu? |

İkisi de main ile çakışmasız. Önce #2, sonra #3.

---

## 🔴 2. HATA: Hakaret mesajında kapı açılabiliyor

Gerçek `qwen2.5:7b` ile yapılan testte (oyunun isteğinin birebir aynısı, `Docs/Tests/llm_diyalog_sonuclari.json`):

> Oyuncu: *"Seni yaşlı bunak, hemen çekil yolumdan yoksa kapıyı kırarım!"*
> Model (3 denemeden 1'inde): `{"response": "Sessizlikten bahsetmiştik... Ben sessizlik.", "action": "open_door"}`

`GameManager.HandleNpcActionRequested` sadece aşamayı kontrol ettiği için **oyunda kapı açılır**. Sunumda biri hakaret ederse kapının açılması kötü görünür.
Model ayrıca cevabı ("sessizlik") neredeyse her yanıtta kendisi söylüyor (ipucu, rüşvet, alakasız sorularda bile).

**Önerilen çözüm (Unity tarafında, LLM'e güvenmeden):** `open_door` ancak **oyuncunun kendi mesajında** kabul edilen cevap geçiyorsa kabul edilsin.

`NpcPersona.cs`'e:
```csharp
[SerializeField] private string[] acceptedAnswers = { "sessizlik", "sükut", "sükût" };
public IReadOnlyList<string> AcceptedAnswers => acceptedAnswers;
```

`GuardNPC.HandlePlayerMessageAsync` içinde, `TryParse`'tan sonra ve `ActionRequested` çağrılmadan önce:
```csharp
if (decision.Action == NpcAction.OpenDoor && !PlayerGaveAnswer(message))
{
    Debug.LogWarning($"LLM open_door üretti ama oyuncu cevabı vermedi; reddedildi. Mesaj: {message}", this);
    decision = new NpcDecision(decision.Dialogue, NpcAction.None);
}
```
```csharp
private bool PlayerGaveAnswer(string playerMessage)
{
    string text = playerMessage.ToLower(new System.Globalization.CultureInfo("tr-TR"));
    foreach (string answer in persona.AcceptedAnswers)
    {
        if (text.Contains(answer)) return true;
    }
    return false;
}
```
- Cevap sızıntısı için: NPC yanıtında cevap kelimesi geçiyorsa ve oyuncu cevabı vermediyse kelimeyi `"..."` ile değiştirebilirsin (görevlerindeki "cevap sızıntısı filtresi").
- **PlayMode testi ekle:** Mock LLM `open_door` döndürsün, oyuncu mesajı "merhaba" olsun → kapı **açılmamalı**.
- README 5.5 ve 9.3'teki "gözlenen sorunlar" maddesini çözümle birlikte güncelle (README benim, metni bana gönder ya da PR'da yaz, eklerim).

---

## 🔴 3. Duraklatma API'si (Ömer bekliyor) (≈20 dk)

Duraklatma menüsünü bu API main'e girmeden yazamıyorum. Senin daha önce söylediğin imzalar:
```csharp
// GameManager
public bool IsPaused { get; private set; }
public event Action<bool> PausedChanged;
public void SetPaused(bool paused); // Time.timeScale 0/1, girdiyi kilitle, imleci serbest bırak/kilitle, PausedChanged

// PlayerController
mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", mouseSensitivity); // Awake/Start
public void SetMouseSensitivity(float value);
```
- Anahtar adı `"MouseSensitivity"` (`GameSettings.SensitivityKey` ile aynı), değiştirme.
- Main'e girince haber ver. PauseMenu prefab'ını (`Assets/Art/Prefabs/PauseMenu.prefab`) ~1 saatte hazırlayıp sana veririm, sen Canvas altına koyarsın.

---

## 🟠 4. Sahne işleri (`Main.unity` sende)

- [ ] **Skybox:** *Lighting → Environment → Skybox Material* = `Assets/Art/Skybox/TempleSky.mat`
- [ ] **Zafer paneli:** VictoryPanel'e `MainMenuButton` ("Ana Menü") ekle → `GameHUD` → `Main Menu Button` alanına bağla. `SceneBuilder.BuildUi`'ye de ekle.
- [ ] **SceneBuilder Build Settings'i eziyor:** `SceneBuilder.cs` ~250. satır `EditorBuildSettings.scenes = new[] { Main }` yapıyor → sahne yeniden kurulunca **MainMenu listeden silinir**. Şöyle olmalı:
  ```csharp
  EditorBuildSettings.scenes = new[]
  {
      new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
      new EditorBuildSettingsScene(ScenePath, true)
  };
  ```
- [ ] PauseMenu prefab'ı (Madde 3'ten sonra).

---

## 🟠 5. Otonom ajan başarı testi (ders dokümanı 4.4, zorunlu)

> *"Otonom kahramanın davranışı, farklı başlangıç konumlarında/koşullarda tekrarlanabilir biçimde test edilmelidir. Test sonuçları (başarı oranı, ortalama tamamlama süresi vb.) raporda sunulmalıdır."*

- `GameFlowTests`'teki `Bolum1_Yoldas_SensorleSalteriBulur...` testini **parametreli** yap (`[ValueSource]` ile 4–5 farklı başlangıç konumu). Her konumda: başarılı mı, kaç saniyede köprü indi.
- En az bir "başarısız olmalı" durumu ekle: şalteri görmediği bir konum ("Beni başka bir noktaya götür" demeli).
- Sonuçları README **9.2** tablosuna yaz:

  | Başlangıç konumu | Deneme | Başarı | Ort. süre |
  |---|---|---|---|

---

## 🟠 6. Build (teslim paketinde zorunlu) (≈10 dk)

- *File → Build Profiles → Windows* → Scene List: **0 MainMenu, 1 Main** → **Build** → ör. `Build/YankiTapinagi.exe`.
- `Build/` klasörünü **repoya koyma** (zip'e koyacağız). Gerekirse `.gitignore`'a `/Build/` ekle.
- Build'i bir kez çalıştır: menü → oyun → muhafız (Ollama açık) → zafer.

---

## 🟠 7. README'deki senin bölümlerin

README'de `<!-- TODO (Erdem) ... -->` yorumlarıyla işaretli. Metinleri bana gönder ya da PR'da düzenle:
- [ ] **1.3** Zorluk mekanikleri (eklediysen; yoksa bu satırı sil)
- [ ] **5.5** LLM güvenliği: Madde 2'deki düzeltme + cevap sızıntısı filtresi
- [ ] **8.1** Yeni sınıflar varsa sınıf diyagramına ekle
- [ ] **9.2** Ajan başarı tablosu (Madde 5)
- [ ] **10** Test Runner ekran görüntüsü (tüm testler yeşil) → `Docs/Images/08_testler.png`
- [ ] Ekip satırında isimler doğru mu? (Erdem Şahin · Ömer Demirel) Öğrenci numaraları gerekiyorsa ekle.

---

## 🔵 8. Zamanı kalırsa: zorluk mekanikleri (zorunlu değil)

`GOREVLER_ERDEM.md`'deki çift kilit (basınç plakası + `AndGate`), hareketli engel, Tapınak Mührü, deneme hakkı. **Madde 1–7 bitmeden başlama.** Ders isterleri bunlar olmadan da karşılanıyor.

---

## Küçük notlar
- Unity test çalıştırınca `ProjectSettings/ProjectSettings.asset`'e varsayılan değerler yazıyor (`targetPixelDensity`, iOS/macOS sürüm alanları). Zararsız. Senin alanın olduğu için ben commit'lemedim, istersen sen commit'le.
- Unity `taskapi.slnx` IDE dosyası üretiyor → `.gitignore`'a `*.slnx` ekler misin?
- Son durum: PlayMode testleri **22 → 21 geçti, 0 kaldı, 1 atlandı** (gerçek Ollama testi `[Ignore]`). Gerçek LLM: 7 senaryo × 3 → **21/21 geçerli JSON, 20/21 doğru aksiyon** (tek hata Madde 2).

## Son gün birlikte
1. Tüm PR'lar main'de → ikimiz de `git pull`.
2. Tam akış: Menü → Bölüm 1 → Bölüm 2 → Zafer → Ana Menü → tekrar Başla.
3. Ollama kapalıyken oyun çökmüyor, "düşünüyor..." ve hata mesajı görünüyor.
4. Ekran görüntüleri README'de, testler yeşil.
5. Zip: proje + build + (README zaten GitHub'da) → teslim.
