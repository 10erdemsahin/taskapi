# Yapılacaklar — Görev Dağılımı (Ömer & Erdem)

> 📄 **Ayrıntılı dosyalar:**
> [KURULUM.md](KURULUM.md) — her iki bilgisayarda yapılacak kurulum + günlük Git akışı ve kurallar
> [GOREVLER_ERDEM.md](GOREVLER_ERDEM.md) — Erdem'in adım adım görevleri
> [GOREVLER_OMER.md](GOREVLER_OMER.md) — Ömer'in adım adım görevleri
> **Repo:** https://github.com/10erdemsahin/taskapi

Projenin şu anki durumu: Oynanabilir prototip hazır (Bölüm 1: NavMesh yoldaş + köprü, Bölüm 2: Ollama NPC + kapı).
Kalan işler: **texture**, **menüler**, **harita duvarları**, **toplama görevi + ipucu**, **görevleri zorlaştırma**, **README / rapor**.

---

## Güncel Durum (5 Kasım)

| İş | Kim | Durum |
|---|---|---|
| Git, GitHub, LFS, Smart Merge kurulumu | Erdem | ✅ Bitti |
| Ollama + `qwen2.5:7b` (Erdem'in bilgisayarı) | Erdem | ✅ Bitti |
| Texture pack + materyaller + TempleSky skybox materyali | Ömer | ✅ PR #1 ile main'de |
| Ana menü kodu (`omer/ana-menu`) | Ömer | 🟡 Kod hazır — `MainMenu.unity` + Build Settings commit'i ve PR eksik |
| Duraklatma API'si, fare hassasiyeti, SceneBuilder düzeltmesi (`erdem/duraklatma-api`) | Erdem | 🟡 Kod hazır — testler çalıştırılıyor, sonra PR |
| Harita duvarları | Erdem (+ Ömer materyal) | ⬜ Yeni |
| **Düşmeyi engelleme** (uçurum bariyeri, köprü kapısı, köprü korkulukları) | Erdem (+ Ömer korkuluk görseli) | ⬜ Yeni |
| Toplama görevi (Yankı Taşı parçaları) + ipucu | Ömer (çekirdek) + Erdem (sahne/kapı) | ⬜ Yeni |
| Bölüm 1 / Bölüm 2 zorlukları | Erdem | ⬜ |
| Duraklatma menüsü | Ömer | ⬜ (`SetPaused` main'e girince) |
| README / rapor | Ömer (+ Erdem metinleri) | ⬜ |

---

## Kim Ne Yapıyor

İşler, iki kişi **aynı dosyaya aynı anda dokunmayacak** şekilde bölündü:

| | **Ömer** — Görsel, Menü, Toplama Görevi, Doküman | **Erdem** — Sahne, Oynanış, Zorluk, Test |
|---|---|---|
| Ana işler | Texture + duvar materyali · Ana menü · Duraklatma menüsü · **Toplama görevi sistemi** (parça, görev sayacı, ipucu, HUD) · README/rapor | **Harita duvarları** · Toplama görevini **sahneye yerleştirme ve kapıya bağlama** · Bölüm 1–2 zorlukları · Testler · SceneBuilder |
| Sahibi olduğu klasörler | `Assets/Art/`, `Assets/Materials/`, `Assets/Scripts/UI/`, **`Assets/Scripts/Quest/`**, **`Assets/Data/Quest/`**, `Assets/Scenes/MainMenu.unity`, Build Settings (sahne sırası), `README.md`, `Docs/` | `Assets/Scripts/` (UI ve Quest hariç), `Assets/Editor/`, `Assets/Tests/`, `Assets/Data/` (Quest hariç), `Assets/Scenes/Main.unity`, diğer `ProjectSettings/`, `Packages/` |
| Dokunmaması gereken | `Main.unity`, `GameManager.cs`, `SceneBuilder.cs`, `Core/` | `Scripts/UI/`, `Scripts/Quest/`, `Materials/`, `MainMenu.unity`, `README.md` |

> **Neden böyle?** Unity sahne dosyaları (`.unity`) birleştirilmesi en zor dosyalardır.
> `Main.unity`'yi yalnızca Erdem değiştirir. Ömer, sahneye girecek her şeyi **prefab veya materyal** olarak hazırlar, Erdem yerleştirir.

---

## Yeni İş 1 — Harita Duvarları

Şu an iki zeminin kenarları açık; oyuncu yan ve arka kenarlardan düşebiliyor, alan "tapınak" gibi durmuyor.

- **Erdem:** Her iki alanın **sol, sağ ve arka** kenarına 3 m yüksek duvar (uçurum tarafı **açık** kalır — fiziksel engel bozulmasın). NavMesh yeniden bake, SceneBuilder'a ekleme, yoldaşın görüşünü bozmadığını test etme.
- **Ömer:** Uzun duvarlarda texture'ın uzamaması için ayrı **`Wall.mat`** (taş/tuğla, uygun tiling). Erdem duvarlara atar.

## Yeni İş 1B — Düşmeyi Engelleme

Oyuncu **hiçbir yerden aşağı düşememeli**, ama köprü inmeden uçurumu yine **geçememeli** (fiziksel engel bozulmaz).

- **Erdem:** Uçurum kenarlarına **görünmez 3 m'lik bariyerler** (Layer Ignore Raycast → NavMesh'e ve yoldaşın görüşüne karışmaz). Köprünün önündeki parça (`BridgeGate_A`) **köprü inince kapanır** (`DisableOnActivated` — DrawBridge koduna dokunmadan). Köprünün iki yanına köprüyle birlikte dönen görünmez korkuluklar. KillZone güvenlik ağı olarak kalır. Testler + SceneBuilder.
- **Ömer:** Görünen kısım: `StoneRailing.prefab` (taş, 2 m × 1 m) ve `BridgeRailing.prefab` (ahşap, 1 m × 1 m), **collider'sız**, sadece görsel.

## Yeni İş 2 — Toplama Görevi: "Yankı Taşı Parçaları"

**Kurgu:** Tapınağın 5 Yankı Taşı parçası haritaya dağılmış. Hepsi toplanınca taşlar birleşir ve oyuncuya **muhafızın bilmecesi için bir ipucu** fısıldar. **Parçalar tamamlanmadan muhafız kapıyı açmaz** (doğru cevap verilse bile).

**Neden zorlaştırıyor?** Oyuncu artık sadece bilmeceyi bilmekle geçemez: haritayı keşfetmeli, bir parçayı **yoldaşa aldırmalı** (yüksek platformda), bir parçaya köprüden sonra ulaşabilmeli.

| Parça | Önerilen yer | Nasıl alınır |
|---|---|---|
| 1 | Alan A, platformun arkası (uçurum kenarı) | Oyuncu [E] |
| 2 | Alan A, sağ arka köşe (duvar dibinde, kutu arkasında) | Oyuncu [E] |
| 3 | **Yüksek platformun üstü** (şalterin yanı) | **Sadece yoldaş** [F] |
| 4 | Alan B, muhafızın arkası | Oyuncu [E] (köprüden sonra) |
| 5 | Alan B, sol duvar dibinde **0.9 m'lik kaya üstü** | Oyuncu zıplayarak [E] |

**İş bölümü (sözleşme önce yazılır, sonra ikisi paralel çalışır):**
- **Erdem (1. gün):** `Assets/Scripts/Core/Interfaces/ICollectionQuest.cs` arayüzünü yazar ve main'e alır (aşağıda). Sonra: GameManager'da kapıyı parçalara bağlar, NPC'ye "parçalar toplandı mı" bilgisini verir, parçaları sahneye yerleştirir, testleri yazar.
- **Ömer:** `Collectible` (toplanabilir parça), `CollectionQuest` (sayaç + ipucu), ipucu verisi (ScriptableObject), parça prefab'ı (parlayan taş + toplama efekti/sesi), HUD'da **"Yankı Taşı: 3/5"** sayacı ve ipucu paneli.

```csharp
// Assets/Scripts/Core/Interfaces/ICollectionQuest.cs  (Erdem yazar — ikisi de buna göre kodlar)
using System;

namespace AdventureGame
{
    public interface ICollectionQuest
    {
        int CollectedCount { get; }
        int RequiredCount { get; }
        bool IsCompleted { get; }
        string HintText { get; }                 // tamamlanınca gösterilecek ipucu

        event Action<int, int> ProgressChanged;  // (toplanan, gereken)
        event Action<string> Completed;          // ipucu metniyle birlikte, bir kez
    }
}
```

---

## 0. Ortak Başlangıç

- [x] **Git kurulumu:** yerel repo, Unity `.gitignore`, Git LFS, Smart Merge, ilk commit — yapıldı.
- [x] **Unity ayarları:** Force Text + Visible Meta Files — zaten açık.
- [x] **GitHub'a gönder + Ömer'i ekle** (Erdem).
- [ ] **Her iki bilgisayarda kurulum** → [KURULUM.md](KURULUM.md).
- [ ] **Branch düzeni:** herkes kendi dalında çalışır → `omer/...`, `erdem/...`. `main`'e sadece PR ile girilir.

---

## 1. ÖMER — Özet (ayrıntı: [GOREVLER_OMER.md](GOREVLER_OMER.md))
- [x] Texture pack + materyaller + skybox materyali
- [ ] `Wall.mat` (duvarlar için)
- [ ] Korkuluk prefab'ları: `StoneRailing`, `BridgeRailing` (düşme engeli görseli)
- [ ] Ana menü: `MainMenu.unity` + Build Settings commit'i + PR
- [ ] **Toplama görevi sistemi:** `Collectible`, `CollectionQuest`, ipucu verisi, parça prefab'ı, HUD sayacı + ipucu paneli
- [ ] Duraklatma menüsü (Erdem'in `SetPaused`'u main'e girince)
- [ ] README + rapor

## 2. ERDEM — Özet (ayrıntı: [GOREVLER_ERDEM.md](GOREVLER_ERDEM.md))
- [ ] Adım 1: `SetPaused`, fare hassasiyeti, SceneBuilder düzeltmesi → testler → PR (kod hazır)
- [ ] **`ICollectionQuest` arayüzü** → main'e (Ömer'i bekletmesin)
- [ ] **Harita duvarları** + NavMesh bake + SceneBuilder
- [ ] **Düşmeyi engelleme:** uçurum bariyerleri, köprü kapısı, köprü korkulukları, testler
- [ ] **Toplama görevini sahneye koyma ve kapıya bağlama** (parçalar yokken `open_door` reddedilir)
- [ ] Bölüm 1 zorlukları (çift kilit, hareketli engel, gizli şalter)
- [ ] Bölüm 2 zorlukları (iki bilmece, deneme hakkı, cevap sızıntısı filtresi)
- [ ] Testler + README metinleri

---

## 3. Birleştirme ve Teslim (son gün, birlikte)

- [ ] İki dalı `main`'e birleştir, çakışmaları çöz.
- [ ] Tam oyun akışı: Menü → Bölüm 1 (parçalar + köprü) → Bölüm 2 (parçalar + ipucu + muhafız) → Zafer → Ana Menü.
- [ ] Hiçbir yerden aşağı düşülmüyor (uçurum kenarı, köprü yanları, harita kenarları).
- [ ] Hata senaryoları: Ollama kapalıyken oyun çökmüyor, NPC "düşünüyor..." gösteriyor.
- [ ] Tüm testler yeşil.
- [ ] README ve rapor son kontrol, ekran görüntüleri güncel.

---

## Özet Takvim (öneri)

| Gün | Ömer | Erdem |
|---|---|---|
| 1 | Ana menü sahnesi + PR, `Wall.mat` | Adım 1 PR'ı, **`ICollectionQuest` arayüzü**, harita duvarları |
| 2 | Korkuluk prefab'ları, toplama görevine başla | **Düşme engeli** (bariyer, köprü kapısı, korkuluk) |
| 3 | **Toplama görevi** bitir (Collectible, CollectionQuest, prefab, HUD sayacı, ipucu paneli) | Bölüm 1 zorlukları (çift kilit, hareketli engel) |
| 4 | Duraklatma menüsü | Parçaları sahneye yerleştirme + kapıya bağlama + NPC bilgisi |
| 5 | Zafer paneli butonu, cilalama | Bölüm 2 zorlukları (iki bilmece, deneme hakkı, sızıntı filtresi) |
| 6 | README taslağı, ekran görüntüleri | Testler, SceneBuilder güncellemesi, mimari metni |
| 7 | **Birlikte:** birleştirme, tam test, teslim | |
