# Erdem için: Ömer'in yeni asset'lerini sahneye bağlama

Bunlar `omer/gorsel-son` dalında (PR'a bak). Kodun ve asset'lerin hepsi hazır. Sahne (`Main.unity`) ve `SceneBuilder` sende olduğu için bağlama işi senin. Her madde birkaç dakikalık iş. **Önerilen sıra: 1 → 2 → 4 → (vakit varsa) 3.**

Asset'ler yeniden üretilebilir: *Tools → AdventureGame → Ömer → Görsel Asset'leri Üret*.

---

## 1. Duvar materyali: `Assets/Materials/Wall.mat` (1 dk)
- Harita kenarındaki duvarlara (yan ve arka) `Stone.mat` yerine `Wall.mat` ata.
- Texture: Bricks113B (2K), yosunlu, düzensiz taş blok. Platformdan ayırt edilsin diye biraz koyu tonlu.
- Tiling materyalde `1,1`. Yoğunluğu senin `WorldSpaceUV` belirliyor; taş için 1.5 m iyi görünür.
- SceneBuilder'da: `AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Wall.mat")`.

## 2. Korkuluk görselleri (5–10 dk)
Prefab'larda **collider yok**, sadece görsel; çarpışmayı senin görünmez bariyerlerin yapıyor. Pivot **alt-orta** (y = 0 zemin).

| Prefab | Boyut | Nereye |
|---|---|---|
| `Assets/Art/Prefabs/StoneRailing.prefab` | 2 m × 1 m × 0.3 m | Uçurum kenarı (z ≈ 9.8 ve z ≈ 17.2) boyunca, köprü genişliği hariç. 2 m arayla diz. |
| `Assets/Art/Prefabs/BridgeRailing.prefab` | 1 m × 1 m | Köprünün iki yanına, köprü objesinin **çocuğu** olarak (köprüyle birlikte dönsün). 7–8 tane. |

SceneBuilder örneği:
```csharp
var stoneRail = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Prefabs/StoneRailing.prefab");
for (float x = -9f; x <= 9f; x += 2f)
{
    if (Mathf.Abs(x) < 2.5f) continue; // köprü geçidi açık kalsın
    var r = (GameObject)PrefabUtility.InstantiatePrefab(stoneRail, parent);
    r.transform.position = new Vector3(x, 0f, 9.8f);
}
```

## 3. Yankı Taşı toplama görevi (isteğe bağlı, ~5 dk + test)
Kod, prefab, ses, ipucu verisi ve HUD hazır ve testten geçti (geçici PlayMode testi, bkz. PR):
- **Kod:** `Assets/Scripts/Quest/`: `Collectible`, `CollectionQuest : ICollectionQuest`, `FloatingSpin`, `QuestHintData`.
- **Prefab'lar:** `EchoShard.prefab` (Interactable katmanında, katı küçük collider) ve `ShardPickupFX.prefab`.
- **Veri ve ses:** `Assets/Data/Quest/EchoStoneHint.asset` ve `Assets/Art/Audio/shard_pickup.wav`.
- **HUD (`QuestHUD`):** Sağ üstte "Yankı Taşı: 2/5" sayacı; tamamlanınca ipucu paneli 8 sn açılır, [H] tekrar gösterir. `GameHUD` bunu **kendisi ekliyor**, Canvas'ta obje gerekmez. Sahnede görev yoksa hiçbir şey göstermez.

**Sahneye koymak için:** `Main.unity` açıkken *Tools → AdventureGame → Ömer → Yankı Taşı Parçalarını Açık Sahneye Yerleştir* menüsünü çalıştır. Bu komut:
- 5 parça koyar: Alan A'da 3, yüksek platformda 1 (**yalnızca Robo alabilir**, `allowPlayer = false`), Alan B'de 1.
- `CollectionQuest` ekler ve `GameManager.collectionQuestBehaviour`'a bağlar.
- Sahneyi **kaydetmez**. Bakıp beğenirsen sen kaydet, ya da aynı kodu SceneBuilder'a taşı.

⚠ **Dikkat (kendi testlerin):**
- Platformdaki parça Robo için ikinci bir hedef olur. Sensör en kısa yolu seçtiği için [F]'de önce parçayı alıp ikinci [F]'de şaltere gidebilir.
- Bu davranış `AgentSuccessTests` süre ve başarı sayılarını değiştirebilir. Koyduktan sonra testleri çalıştır. Sorun olursa platform parçasını kaldır, ya da `allowAICompanion = false` yapıp yere koy.
- Alan A'daki parçalarda `allowAICompanion = false`, yani Robo onlarla ilgilenmez.
- Vakit yoksa **koyma**: görev bağlı değilken kapı kuralı devre dışı, oyun aynen oynanır.

## 4. Texture kalitesi (yapıldı, bilgin olsun)
- Zemin (Ground068) ve taş (Bricks113A) **2K** oldu. GUID'ler korunduğu için materyal ve sahne referansları bozulmadı.
- Tüm texture'larda Aniso 8, Max Size 2048, Compression High Quality.
- Normal map şiddeti 1.15–1.3, taş smoothness 0.2, ahşap 0.25.
