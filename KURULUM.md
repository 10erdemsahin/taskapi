# Kurulum — Her İki Bilgisayarda Yapılacaklar

Bu dosyayı **Ömer de Erdem de** kendi bilgisayarında baştan sona uygular.
Kişiye özel görevler: [GOREVLER_ERDEM.md](GOREVLER_ERDEM.md) · [GOREVLER_OMER.md](GOREVLER_OMER.md) · Genel plan: [YAPILACAKLAR.md](YAPILACAKLAR.md)

**Repo:** https://github.com/10erdemsahin/taskapi
**Unity sürümü:** `6000.6.7f1` (tam olarak bu, başka sürüm KULLANMAYIN)

---

## 1. Programları Kur

| # | Program | Nereden | Not |
|---|---|---|---|
| 1 | **Unity Hub** | unity.com/download | Unity hesabıyla giriş yap, Personal lisansı aktif et |
| 2 | **Unity 6000.6.7f1** | Hub → Installs → Install Editor → *Archive* sekmesi | Modül olarak **Microsoft Visual Studio Community** seçilebilir |
| 3 | **Git** | git-scm.com | Kurulumda varsayılan ayarlar yeterli |
| 4 | **Git LFS** | git-lfs.com | Git for Windows ile çoğu zaman birlikte gelir, kontrol et (aşağıda) |
| 5 | **Ollama** | ollama.com/download | Muhafız NPC'nin gerçek sohbeti için gerekli |
| 6 | (İsteğe bağlı) **GitHub Desktop** | desktop.github.com | Komut satırı yerine arayüzle çalışmak isteyen için |

Kurulumları kontrol et (terminal / Git Bash):
```bash
git --version
```
```bash
git lfs version
```
```bash
ollama --version
```

## 2. Git Kimliğini Ayarla (bir kez)

Commit'lerde kimin ne yaptığı görünsün diye. **Herkes kendi adını ve GitHub e-postasını yazar.**
```bash
git config --global user.name "Ad Soyad"
```
```bash
git config --global user.email "github-epostan@ornek.com"
```
```bash
git lfs install
```

## 3. Repoya Erişim

- **Erdem:** repo sahibi, ek bir şey gerekmez.
- **Ömer:** Erdem'in gönderdiği **davet e-postasını kabul et** (veya github.com/notifications). Kabul etmeden `push` yapamazsın.
- İlk `push`/`clone` sırasında tarayıcıda GitHub girişi açılır → giriş yap (Git Credential Manager şifreyi hatırlar).

## 4. Projeyi Bilgisayara İndir (Clone)

> ⚠ **Kısa bir klasör yolu kullan.** Windows'ta 260 karakter yol sınırı var; uzun yollarda Unity derleme hatası verir.
> İyi: `C:\Projeler\taskapi` · Kötü: `C:\Users\...\OneDrive\Belgeler\Okul\Dersler\...`
> **OneDrive / Masaüstü senkronize klasörlerine KOYMAYIN** (Library klasörü senkronizasyonu kilitler).

```bash
git clone https://github.com/10erdemsahin/taskapi.git C:/Projeler/taskapi
```
```bash
git -C C:/Projeler/taskapi config core.longpaths true
```

**Erdem için not:** Proje şu an `Desktop\YankiTapinagi` klasöründe ve repo zaten oraya bağlı. Önce [GOREVLER_ERDEM.md](GOREVLER_ERDEM.md) → *Adım 0*'ı yap (ilk push). İstersen sonra yukarıdaki gibi kısa bir yola yeniden clone'la.

## 5. Unity Smart Merge Ayarı (her bilgisayarda, her clone'da bir kez)

Sahne/prefab çakışmalarını Unity'nin kendi aracı çözsün diye. Bu ayar **repo ile gelmez**, herkes kendisi yapar:
```bash
git -C C:/Projeler/taskapi config merge.unityyamlmerge.name "Unity SmartMerge"
```
```bash
git -C C:/Projeler/taskapi config merge.unityyamlmerge.driver "'C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Data/Tools/UnityYAMLMerge.exe' merge -p %O %B %A %A"
```
```bash
git -C C:/Projeler/taskapi config merge.unityyamlmerge.recursive binary
```
> Unity başka bir klasöre kuruluysa yolu ona göre değiştir (Hub → Installs → ⋮ → Show in Explorer).

## 6. Projeyi Unity'de Aç

1. Unity Hub → **Add → Add project from disk** → `C:\Projeler\taskapi` klasörünü seç.
2. Sürüm olarak **6000.6.7f1** seçili olmalı.
3. İlk açılış **5–10 dakika** sürer (paketler indirilir, `Library` oluşturulur). İnternet bağlı olsun.
4. *Safe Mode* sorusu çıkarsa → **Ignore** deme; Console'daki hatayı oku, takım arkadaşına sor.
5. `Assets/Scenes/Main.unity` → çift tık → **Play**.

**Kontroller:** WASD yürü · Shift koş · Space zıpla · Fare bak · **F** yoldaştan yardım · **E** etkileşim / konuş · **Esc** diyaloğu kapat

## 7. Ollama'yı Hazırla (muhafız sohbeti için)

```bash
ollama pull qwen2.5:7b
```
- Ollama uygulaması açıkken sunucu otomatik çalışır (sağ alt köşede lama simgesi).
- Kontrol: tarayıcıda `http://localhost:11434` → "Ollama is running" yazmalı.
- Zayıf bilgisayar (8 GB RAM altı) için: `ollama pull llama3.2:3b` ve sahnede **Services → OllamaLLMService → Model** alanını `llama3.2:3b` yap. **Bu değişikliği commit'leme** (herkesin modeli farklı olabilir).
- Ollama olmadan test: **GuardNPC → Llm Service Behaviour** alanına `Services` objesindeki **MockLLMService**'i sürükle (doğru cevap: "sessizlik"). Bu da **commit'lenmez**, test sonrası geri al.
- Oyunda sağ üstteki "LLM:" yazısı yeşilse bağlantı tamam.

## 8. Testleri Çalıştır

*Window → General → Test Runner → PlayMode → Run All*. Her şey yeşil olmalı. Kırmızı test varsa push yapma, önce düzelt.

---

## 9. Günlük Çalışma Akışı (KURALLAR)

### Her çalışmaya başlarken
```bash
git checkout main
```
```bash
git pull
```
```bash
git checkout -b omer/menu-ekrani
```
(dal adı: `omer/...` veya `erdem/...` + kısa iş adı)

### Çalışırken
- **Unity açıkken** dosyaları Explorer'dan taşıma/silme; her şeyi Unity'nin Project penceresinden yap (`.meta` dosyaları bozulmasın).
- Her dosyanın `.meta` dosyası da commit'e girmeli. `.meta`'sız dosya = başkasında kırık referans.
- Sık ve küçük commit at:
```bash
git add -A
```
```bash
git commit -m "Ana menüye Ayarlar paneli eklendi"
```
```bash
git push -u origin omer/menu-ekrani
```

### İş bitince
1. GitHub'da **Pull Request** aç: `omer/...` → `main`.
2. Takım arkadaşı PR'ı açıp bakar, **kendi bilgisayarında dalı çekip Play'e basarak** dener.
3. Onaylanınca **Merge**. Sonra herkes `git checkout main` + `git pull`.

### Altın kurallar
1. **`main`'e doğrudan push yok.** Her şey PR ile girer.
2. **Kendi klasörünün dışına dokunma** (sahiplik tablosu [YAPILACAKLAR.md](YAPILACAKLAR.md)'de). Başkasının dosyasında değişiklik gerekiyorsa ona yaz, o yapsın.
3. **`Assets/Scenes/Main.unity` sadece Erdem'de.** Ömer bu sahneyi açıp Play'e basabilir ama **kaydetmez** (değişiklik olduysa: `git checkout -- Assets/Scenes/Main.unity`).
4. **`ProjectSettings/` değişikliğini haber ver.** Build Settings (sahne sırası) Ömer'de, diğer proje ayarları Erdem'de.
5. Unity sürümünü yükseltme, paket ekleme/çıkarma → **önce konuşun.**
6. Büyük dosyalar (texture, model, ses) **otomatik LFS'e** gider; yine de texture'ları **2K'dan büyük** indirmeyin (GitHub LFS ücretsiz kota: 1 GB depolama + aylık 1 GB indirme).
7. Push'tan önce: Console'da kırmızı hata yok, testler yeşil.
8. Çakışma (conflict) çıkarsa paniğe kapılma, **zorla push (`--force`) yapma**, takım arkadaşınla birlikte çöz.

### Bir dosyayı yanlışlıkla değiştirdim, geri almak istiyorum
```bash
git checkout -- yol/dosya.unity
```

---

## 10. Sık Karşılaşılan Sorunlar

| Sorun | Çözüm |
|---|---|
| Texture'lar pembe/boş, dosyalar 1 KB | Git LFS kurulu değil → `git lfs install` sonra `git lfs pull` |
| `Insecure connection not allowed` | Project Settings → Player → *Allow downloads over HTTP* = **Always allowed** (repo'da zaten ayarlı) |
| `Unity.AI.Navigation` bulunamadı | Package Manager'da AI Navigation **2.0.16** yüklü mü? |
| Derleme sırasında "path not found" | Proje yolu çok uzun → kısa yola taşı (bkz. Adım 4) |
| Muhafız "Ollama'ya ulaşılamadı" diyor | Ollama uygulaması açık mı? `ollama list` ile model yüklü mü? |
| İlk mesaj çok uzun sürüyor | Model ilk kez belleğe yükleniyor (10–30 sn), normal |
| `push` reddedildi (rejected) | Önce `git pull`, çakışma varsa çöz, sonra tekrar push |
| Ömer push yapamıyor (403) | Collaborator davetini kabul etmemiş |
