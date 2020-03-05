using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using Random = UnityEngine.Random;

public class BoardManager : MonoBehaviour
{
    [Serializable]
    public class Count {
        public int minimum;
        public int maximum;

        public Count (int min, int max) {
            minimum = min;
            maximum = max;
        }
    }

    [Serializable]
    public class Atmosphere {
        public float r;
        public float g;
        public float b;
        public float a;

        public Atmosphere (float r, float g, float b, float a = 0f) {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }
    }

    public const int columns = 8;
    public const int rows = 8;
    public readonly Atmosphere VISION_LIGHT = new Atmosphere(1f, 1f, 1f);
    public readonly Atmosphere FOG_LIGHT = new Atmosphere(.6f, .6f, .6f);
    public readonly Atmosphere NIGHT_LIGHT = new Atmosphere(.1f, .1f, .1f);
    public readonly Atmosphere NIGHT_ATMOSPHERE = new Atmosphere(.4f, .4f, .2f);
    public readonly Atmosphere DUSK_ATMOSPHERE = new Atmosphere(.2f, .3f, .4f);
    public readonly Atmosphere DAWN_ATMOSPHERE = new Atmosphere(.1f, .2f, .3f);
    public const float INVISIBLE = 0f;
    public const float BARELY_VISIBLE = .4f;
    public static readonly ReadOnlyCollection<String> VISION_INVISIBLE_TAGS = new ReadOnlyCollection<String>(new []{""});
    public static readonly ReadOnlyCollection<String> VISION_BARELY_VISIBLE_TAGS = new ReadOnlyCollection<String>(new []{""});
    public static readonly ReadOnlyCollection<String> FOG_INVISIBLE_TAGS = new ReadOnlyCollection<String>(new []{"Enemy"});
    public static readonly ReadOnlyCollection<String> FOG_BARELY_VISIBLE_TAGS = new ReadOnlyCollection<String>(new []{"Food", "Soda"});
    public static readonly ReadOnlyCollection<String> NIGHT_INVISIBLE_TAGS = new ReadOnlyCollection<String>(new []{"Enemy", "Food", "Soda"});
    public static readonly ReadOnlyCollection<String> NIGHT_BARELY_VISIBLE_TAGS = new ReadOnlyCollection<String>(new []{"Wall"});

    public Count wallCount = new Count(5,9);
    public Count foodCount = new Count(1,5);
    public GameObject exit;
    public GameObject[] floorTiles;
    public GameObject[] wallTiles;
    public GameObject[] foodTiles;
    public GameObject[] enemyTiles;
    public GameObject[] outerWallTiles;

    private Transform boardHolder;
    private List<Vector3> gridPositions = new List<Vector3>();
    private List<GameObject> instances = new List<GameObject>();

    void InitialiseList() {
        gridPositions.Clear();

        for (int x = 1; x < columns - 1; x++) {
            for (int y = 1; y < rows - 1; y++) {
                gridPositions.Add(new Vector3(x, y, 0f));
            }
        }
    }

    void BoardSetup() {
        boardHolder = new GameObject("Board").transform;

        for (int x = -1; x < columns + 1; x++) {
            for (int y = -1; y < rows + 1; y++) {
                GameObject instance;
                if(x == -1 || x == columns || y == -1 || y == rows)
                    instance = InstantiateGameObject(outerWallTiles[Random.Range(0, outerWallTiles.Length)], x, y, boardHolder);
                else
                    instance = InstantiateGameObject(floorTiles[Random.Range(0, floorTiles.Length)], x, y, boardHolder);
                instances.Add(instance);
            }
        }
    }

    GameObject InstantiateGameObject (GameObject toInstantiate, int x, int y, Transform holder = null) {
        return InstantiateGameObject(toInstantiate, new Vector3(x, y, 0f), holder);
    }

    GameObject InstantiateGameObject (GameObject toInstantiate, Vector3 v, Transform holder = null) {
        GameObject instance = Instantiate(toInstantiate, v, Quaternion.identity) as GameObject;
        if (holder != null)
            instance.transform.SetParent(boardHolder);
        return instance;
    }

    Vector3 RandomPosition() {
        int randomIndex = Random.Range(0, gridPositions.Count);
        Vector3 randomPosition = gridPositions[randomIndex];
        gridPositions.RemoveAt(randomIndex);
        return randomPosition;
    }

    void LayoutObjetAtRandom(GameObject[] tileArray, int minimum, int maximum) {
        int objectCount = Random.Range(minimum, maximum + 1);

        for (int i = 0; i < objectCount; i++) {
            instances.Add(InstantiateGameObject(tileArray[Random.Range(0, tileArray.Length)], RandomPosition()));
        }
    }

    public void SetupScene(int level) {
        instances.Clear();
        BoardSetup();
        InitialiseList();
        
        LayoutObjetAtRandom(wallTiles, wallCount.minimum, wallCount.maximum);
        LayoutObjetAtRandom(foodTiles, foodCount.minimum, foodCount.maximum);

        int enemyCount = (int)Mathf.Log(level, 2f);
        LayoutObjetAtRandom(enemyTiles, enemyCount, enemyCount);
        
        instances.Add(InstantiateGameObject(exit, columns - 1, rows - 1));
    }

    public void ApplyVision(bool nightTime, Player player, int visionRadius, int fogRadius) {
        if (!instances.Contains(player.gameObject))
            instances.Add(player.gameObject);

        List<GameObject> vision = new List<GameObject>(instances);
        List<GameObject> fog = new List<GameObject>(instances);
        List<GameObject> night = new List<GameObject>(instances);

        int xCenter = (int) player.transform.position.x;
        int yCenter = (int) player.transform.position.y;

        foreach (GameObject i in instances) {
            if (IsInRadius(i.transform.position, xCenter, yCenter, visionRadius)) {
                fog.Remove(i);
                night.Remove(i);
            } else if (IsInRadius(i.transform.position, xCenter, yCenter, fogRadius)) {
                vision.Remove(i);
                night.Remove(i);
            } else {
                vision.Remove(i);
                fog.Remove(i);
            }
        }

        DarkenInstances(vision, VISION_LIGHT, VISION_INVISIBLE_TAGS, VISION_BARELY_VISIBLE_TAGS);
        DarkenInstances(fog, FOG_LIGHT, FOG_INVISIBLE_TAGS, FOG_BARELY_VISIBLE_TAGS);
        if (nightTime)
            DarkenInstances(night, NIGHT_LIGHT, NIGHT_INVISIBLE_TAGS, NIGHT_BARELY_VISIBLE_TAGS);
        else
            DarkenInstances(night, FOG_LIGHT, FOG_INVISIBLE_TAGS, FOG_BARELY_VISIBLE_TAGS);
    }

    private bool IsInRadius(Vector3 position, int xCenter, int yCenter, int radius) {
        int relativeAbsX = (int) Math.Abs(position.x - xCenter);
        int relativeAbsY = (int) Math.Abs(position.y - yCenter);
        return relativeAbsX <= radius
            && relativeAbsY <= radius
            && relativeAbsX + relativeAbsY != 2 * radius;
    }

    private void DarkenInstances(List<GameObject> gameObjects, Atmosphere atmo, ReadOnlyCollection<String> invisibleTags, ReadOnlyCollection<String> barelyVisibleTags) {
        foreach (GameObject go in gameObjects) {
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            float alpha = 1f;
            if (invisibleTags.Contains(go.tag))
                alpha = INVISIBLE;
            else if (barelyVisibleTags.Contains(go.tag))
                alpha = BARELY_VISIBLE;
            if (sr != null) {
                sr.color = new Color(atmo.r, atmo.g, atmo.b, alpha);
            }
        }
    }

    public void ApplyDusk() {
        ApplyAtmosphere(DUSK_ATMOSPHERE);
    }

    public void ApplyNight() {
        ApplyAtmosphere(NIGHT_ATMOSPHERE);
    }
    
    public void ApplyDawn() {
        ApplyAtmosphere(DAWN_ATMOSPHERE);
    }

    private void ApplyAtmosphere(Atmosphere atmo) {
        foreach (GameObject i in instances) {
            SpriteRenderer sr = i.GetComponent<SpriteRenderer>();
            if (sr != null) {
                sr.color = new Color(sr.color.r - atmo.r, sr.color.g - atmo.g, sr.color.b - atmo.b, sr.color.a - atmo.a);
            }
        }
    }
}
