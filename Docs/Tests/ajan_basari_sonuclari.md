# Otonom Yoldaş Başarı Testi (ders dokümanı 4.4)

Otomatik üretildi: `AgentSuccessTests` (PlayMode), 6 Kasım 2026 13:45.
Oyuncu basınç plakasında bekler, [F] ile yardım istenir. Başarı: yoldaş şalteri sensörle bulur,
NavMesh ve platform zıplamasıyla gider, çeker ve köprü iner. Süre [F]'den köprünün inmesine kadardır
(2,5 sn iniş animasyonu dahil). Başarısız olması beklenen konumda yoldaş şalteri sütun yüzünden göremez.

| Başlangıç konumu | Deneme | Başarı | Ort. süre (sn) | Beklenen |
|---|---|---|---|---|
| Basınç plakası yanı (5, 0, 7) | 3 | 3/3 | 7,5 | Başarı ✅ |
| Alan ortası (0, 0, 4) | 3 | 3/3 | 6,4 | Başarı ✅ |
| Sağ kenar (8, 0, 2) | 3 | 3/3 | 8,3 | Başarı ✅ |
| Platform önü (-3, 0, 8) | 3 | 3/3 | 5,4 | Başarı ✅ |
| Oyun başlangıcı, sütun arkası (2, 0, -5) | 3 | 0/3 | — | Başarısızlık ✅ |

**Ulaşılabilir konumlarda toplam:** 12/12 başarılı (%100), ortalama 6,9 sn.
