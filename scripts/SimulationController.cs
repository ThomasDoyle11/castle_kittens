using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimulationController : MonoBehaviour
{
    // Kitten Style Sprites
    public Sprite standardStyle;
    public Sprite darkStyle;
    public Sprite superStyle;
    public Sprite beardStyle;
    public Sprite stylishWinterStyle;
    public Sprite zombieStyle;
    public Sprite roboStyle;
    public Sprite randomStyle;

    // Objects in Scene
    public Transform castleAndPlayers;
    public Transform building;
    public Transform buildingMask;
    public Transform door;
    public Transform[] kittenSpots;
    public Transform insideLeft;
    public Transform inside1;
    public Transform inside2;
    public Transform doorOpen;
    public Transform doorClosed;
    public Transform dayNightBackground;

    public Transform[] diceObj;
    public Transform diceBorders;

    // Prefabs
    public GameObject maskPrefab;
    public GameObject scratchPrefab;
    public GameObject cardChangeTextPrefab;
    public GameObject currencyChangeTextPrefab;
    public GameObject healthChangeTextPrefab;
    public GameObject pointsChangeTextPrefab;

    // Sprites
    public Sprite cardSprite;
    public Sprite healthSprite;
    public Sprite pointSprite;
    public Sprite currencySprite;

    // Locations for animation
    public Vector3 doorOpenPosition;
    public Vector3 doorClosedPosition;
    public Vector3 insideLeftPosition;
    public Vector3 insidePosition1;
    public Vector3 insidePosition2;
    public Vector3[] kittenHomePositions;

    // Objects in Scene fetched at runtime
    public Transform[] kittenObjs;
    public Transform[] kittenBodies;
    public Transform[] kittenInfoChangeTextObj;
    public GameObject[] kittenHalos;

    public Transform[] diceRollerObjs;

    // Simulation options
    public float attackDistanceOffset;
    public float attackSpeed
    {
        get
        {
            return 2 * moveSpeed;
        }
    }
    public float moveSpeed
    {
        get
        {
            return 500 * simulationSpeed;
        }
    }
    public float rotateDieSpeed // deg/s
    {
        get
        {
            return 180 * simulationSpeed;
        }
    }
    public float rotateDayNightSpeed // deg/s
    {
        get
        {
            return 60 * simulationSpeed;
        }
    }
    public float squishSpeed
    {
        get
        {
            return 75 * simulationSpeed;
        }
    }
    public float pauseTime
    {
        get
        {
            return 0.1f / simulationSpeed;
        }
    }
    public float wipeSpeed
    {
        get
        {
            return 1000 * simulationSpeed;
        }
    }
    public float fadeTime
    {
        get
        {
            return 0.2f / simulationSpeed;
        }
    }
    public float fadeStayTime
    {
        get
        {
            return 0.75f / simulationSpeed;
        }
    }
    public float changeInfoTextDelay
    {
        get
        {
            return 0.6f / simulationSpeed;
        }
    }
    public float changeInfoFloatDistance
    {
        get
        {
            return 90;
        }
    }

    public float diceRollSpeed // dice faces / s
    {
        get
        {
            return 12 * simulationSpeed;
        }
    }
    public int numTimesDiceRollPassesValue; // After the dice roll has passed the value it will stop on this amount of times, it will stop next time
    public float diceSpeedRandomness; // 0 <= value < 1
    public float diceRollTimeout // If this time is exceeded on a roll, just set the dice value
    {
        get
        {
            return 4f / simulationSpeed;
        }
    }

    public float simulationSpeed;
    public const float standardSimultionSpeed = 1;
    public const float speedUpMultiplier = 4;
    public const float megaSpeedUpMultiplier = 16000;

    // Simulation signals
    public bool simulationPlaying;
    public int totalSimulations;
    public int totalSimulationsComplete;
    public int numObjectsSimulating;

    public List<Simulation> simulationQueue = new List<Simulation>();
    public List<Simulation> removals = new List<Simulation>();

    public enum ChangeInfoText { Card, Currency, Health, Points }
    public List<Transform> changeInfoTextTransforms = new List<Transform>();

    // Singleton pattern
    public static SimulationController instance;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        kittenObjs = new Transform[kittenSpots.Length];
        kittenBodies = new Transform[kittenSpots.Length];
        kittenInfoChangeTextObj = new Transform[kittenSpots.Length];
        kittenHalos = new GameObject[kittenSpots.Length];
        for (int i = 0; i < GameOptions.instance.numPlayers; i++)
        {
            kittenObjs[i] = kittenSpots[i].Find("Kitten");
            kittenObjs[i].Find("Body").Find("Fill").GetComponent<Image>().color = Player.KittenColour(i);
            kittenObjs[i].Find("Body").Find("Outline").GetComponent<Image>().sprite = GameOptions.instance.playerStyles[i].sprite;
            kittenObjs[i].Find("Body").Find("Outline").GetComponent<Image>().color = Color.white;
            Color newColour = Color.HSVToRGB(Player.allHues[i] / 360, 1, 1);
            kittenSpots[i].Find("Spot").GetComponent<Image>().color = new Color(newColour.r, newColour.g, newColour.b, 0.8f);

            kittenBodies[i] = kittenObjs[i].Find("Body");
            kittenInfoChangeTextObj[i] = kittenObjs[i].Find("InfoChangeText");
            kittenHalos[i] = kittenBodies[i].Find("Halo").gameObject;
        }

        diceRollerObjs = new Transform[diceObj.Length];
        for (int i = 0; i < diceObj.Length; i++)
        {
            diceRollerObjs[i] = diceObj[i].Find("Value").Find("Roller");
        }

        numTimesDiceRollPassesValue = 2;
        diceSpeedRandomness = 0.25f;

        attackDistanceOffset = 80;
        simulationSpeed = standardSimultionSpeed;

        totalSimulations = 0;
        totalSimulationsComplete = 0;
        numObjectsSimulating = 0;

        doorClosedPosition = doorClosed.position;
        doorOpenPosition = doorOpen.position;
        insideLeftPosition = insideLeft.position;
        insidePosition1 = inside1.position;
        insidePosition2 = inside2.position;

        SetSimSpeed(GameOptions.instance.preferredSimSpeed);

        //AddSimulationToQueue(new IEnumerator[] { MoveKitten(0, new Vector2(400, 400)) });
        //AddSimulationToQueue(ShowKittenStatChanges(0, new ChangeInfoText[] { ChangeInfoText.Card, ChangeInfoText.Health, ChangeInfoText.Card, ChangeInfoText.Health }, new int[] { 1, -2, 22, 54 }));
        //AddSimulationToQueue(new IEnumerator[] { MoveKitten(0, new Vector2(500, 500)) });
    }

    // Update is called once per frame
    void Update()
    {
        // If no current simulations are playing, check for a new one to play
        if (!simulationPlaying)
        {
            foreach (Simulation simulation in simulationQueue)
            {
                StartCoroutine(simulation.Simulate());
                if (!simulation.simulateASync)
                {
                    simulationPlaying = true;
                    break;
                }
            }
        }
        else
        {
            foreach (Simulation simulation in simulationQueue)
            {
                if (simulation.SimulationHasFinished())
                {
                    removals.Add(simulation);
                    if (!simulation.simulateASync)
                    {
                        simulationPlaying = false;
                    }
                }
            }
        }

        foreach(Simulation removal in removals)
        {
            simulationQueue.Remove(removal);
        }
        removals.Clear();
    }

    // Simulation functions

    public void SetSimSpeed(int speed)
    {
        speed = Mathf.Clamp(speed, 0, 2);
        if (speed == 0)
        {
            simulationSpeed = standardSimultionSpeed;
        }
        else if (speed == 1)
        {
            simulationSpeed = speedUpMultiplier;
        }
        else if (speed == 2)
        {
            simulationSpeed = megaSpeedUpMultiplier;
        }
        UIController.instance.SetSpeedButtonsObj(speed);
    }

    // For calling simulations

    //public void AddSimulationToQueue(IEnumerator[] coroutines)
    //{
    //    StartCoroutine(Simulate(coroutines, false));
    //}

    public void AddSimulationToQueue(Simulation simulation)
    {
        simulationQueue.Add(simulation);
    }

    private IEnumerator Simulate(IEnumerator[] coroutines, bool isASync)
    {
        //int simulationID = totalSimulations;
        //totalSimulations += 1;
        //while (true)
        //{
        //    if (simulationID == totalSimulationsComplete)
        //    {
        //        if (isASync)
        //        {
        //            totalSimulationsComplete += 1;
        //        }
        //        foreach (IEnumerator coroutine in coroutines)
        //        {
        //            yield return coroutine;
        //        }
        //        if (!isASync)
        //        {
        //            totalSimulationsComplete += 1;
        //        }
        //        break;
        //    }
        //    yield return null;
        //}
        yield return null;
    }

    // Primitive Simulation - must all increase numObjectsSimulating and start and decrease at end

    private IEnumerator MoveObject(Transform transform, Vector3 destination, float offset, float speed)
    {
        numObjectsSimulating += 1;
        Vector3 direction = (destination - transform.position).normalized;
        float distance = Vector3.Distance(destination, transform.position);
        float distanceWithOffset = Mathf.Max(0, distance - offset);
        destination = transform.position + distanceWithOffset * direction;
        int xDirection = Sign(destination.x - transform.position.x);
        int yDirection = Sign(destination.y - transform.position.y);
        if (direction != Vector3.zero)
        {
            while (true)
            {
                transform.position += speed * Time.deltaTime * direction;
                if (Sign(destination.x - transform.position.x) != xDirection || Sign(destination.y - transform.position.y) != yDirection)
                {
                    transform.position = destination;
                    break;
                }
                yield return null;
            }
        }
        numObjectsSimulating -= 1;
    }

    private IEnumerator Wait(float seconds)
    {
        numObjectsSimulating += 1;
        yield return new WaitForSeconds(seconds);
        numObjectsSimulating -= 1;
    }

    private IEnumerator ReorderObject(Transform transform, int index)
    {
        numObjectsSimulating += 1;
        transform.SetSiblingIndex(index);
        yield return null;
        numObjectsSimulating -= 1;
    }

    private IEnumerator ReparentObject(Transform transform, Transform newParent)
    {
        numObjectsSimulating += 1;
        transform.SetParent(newParent, true);
        yield return null;
        numObjectsSimulating -= 1;
    }

    private IEnumerator ReparentAndReorderObject(Transform transform, Transform newParent, int index)
    {
        numObjectsSimulating += 1;
        transform.SetParent(newParent, true);
        transform.SetSiblingIndex(index);
        yield return null;
        numObjectsSimulating -= 1;
    }

    private IEnumerator SetObjectActive(GameObject gameObject, bool isActive)
    {
        numObjectsSimulating += 1;
        gameObject.SetActive(isActive);
        yield return null;
        numObjectsSimulating -= 1;
    }

    private IEnumerator RotateObjectTo(Transform transform, float endRotation)
    {
        numObjectsSimulating += 1;
        if (endRotation != transform.rotation.eulerAngles.z)
        {
            float progress = 0;
            Quaternion endQuaternion = Quaternion.Euler(0, 0, endRotation);
            Quaternion startQuaternion = transform.rotation;
            float totalRotation = Quaternion.Angle(startQuaternion, endQuaternion);
            while (true)
            {
                progress += (rotateDayNightSpeed * Time.deltaTime / totalRotation);
                if (progress < 1)
                {
                    transform.rotation = Quaternion.Slerp(startQuaternion, endQuaternion, progress);
                }
                else
                {
                    transform.rotation = endQuaternion;
                    break;
                }
                
                yield return null;
            }
        }
        numObjectsSimulating -= 1;
    }

    private IEnumerator RotateObjectBy(Transform transform, float rotation)
    {
        numObjectsSimulating += 1;
        if (rotation != 0)
        {
            float progress = 0;
            float startRotation = transform.rotation.eulerAngles.z;
            float endRotation = startRotation + rotation;
            while (true)
            {
                progress += Mathf.Abs(rotateDieSpeed * Time.deltaTime / rotation);
                if (progress < 1)
                {
                    transform.rotation = Quaternion.Euler(0, 0, startRotation + progress * rotation);
                }
                else
                {
                    transform.rotation = Quaternion.Euler(0, 0, endRotation);
                    break;
                }

                yield return null;
            }
        }
        numObjectsSimulating -= 1;
    }

    private IEnumerator SquishObject(Transform transform, float squishFactor, bool useWidth)
    {
        numObjectsSimulating += 1;
        if (squishFactor != 0)
        {
            RectTransform rectTransform = transform.GetComponent<RectTransform>();
            float progress = 0;
            float startScale = useWidth ? rectTransform.sizeDelta.x : rectTransform.sizeDelta.y;
            float endScale = useWidth ? rectTransform.sizeDelta.x * squishFactor : rectTransform.sizeDelta.y * squishFactor;
            float scaleChange = endScale - startScale;
            while (true)
            {
                progress += Mathf.Abs((squishSpeed * Time.deltaTime / scaleChange));
                if (progress < 1)
                {
                    rectTransform.sizeDelta = new Vector2(useWidth ? startScale + progress * scaleChange : rectTransform.sizeDelta.x, useWidth ? rectTransform.sizeDelta.y : startScale + progress * scaleChange);
                }
                else
                {
                    rectTransform.sizeDelta = new Vector2(useWidth ? endScale : rectTransform.sizeDelta.x, useWidth ? rectTransform.sizeDelta.y : endScale);
                    break;
                }

                yield return null;
            }
        }
        numObjectsSimulating -= 1;
    }

    private IEnumerator PlaySound()
    {
        numObjectsSimulating += 1;
        yield return null;
        numObjectsSimulating -= 1;
    }

    private IEnumerator WipeObject(GameObject prefab, Transform parent, float direction)
    {
        numObjectsSimulating += 1;
        Transform wipeMask = Instantiate(maskPrefab, parent.position, Quaternion.Euler(0, 0, direction), parent).transform;
        RectTransform wipeMaskRect = wipeMask.GetComponent<RectTransform>();
        Transform wipeObject = Instantiate(prefab, Vector3.zero, Quaternion.Euler(0, 0, direction), wipeMask).transform;
        RectTransform wipeObjectRect = wipeObject.GetComponent<RectTransform>();
        Vector2 startPos = new Vector2(-wipeObjectRect.sizeDelta.x / 2 - wipeMaskRect.sizeDelta.x, 0);
        Vector2 endPos = -startPos;
        wipeObjectRect.anchoredPosition = startPos;
        while (true)
        {
            wipeObjectRect.anchoredPosition += new Vector2(wipeSpeed * Time.deltaTime, 0);
            if (wipeObjectRect.anchoredPosition.x > endPos.x)
            {
                wipeObjectRect.anchoredPosition = endPos;
                break;
            }
            yield return null;
        }
        Destroy(wipeMask.gameObject);
        numObjectsSimulating -= 1;
    }

    private IEnumerator FadeObjectInAndOut(Transform transform)
    {
        numObjectsSimulating += 1;
        CanvasGroup fadeObjectCanvasGroup = transform.GetComponent<CanvasGroup>();
        fadeObjectCanvasGroup.alpha = 0;
        while (true)
        {
            // Fade in
            fadeObjectCanvasGroup.alpha += Time.deltaTime / fadeTime;
            if (fadeObjectCanvasGroup.alpha >= 1)
            {
                fadeObjectCanvasGroup.alpha = 1;
                break;
            }
            yield return null;
        }
        yield return new WaitForSeconds(fadeStayTime);
        while (true)
        {
            // Fade out
            fadeObjectCanvasGroup.alpha -= Time.deltaTime / fadeTime;
            if (fadeObjectCanvasGroup.alpha <= 0)
            {
                fadeObjectCanvasGroup.alpha = 0;
                break;
            }
            yield return null;
        }
        numObjectsSimulating -= 1;
    }

    private IEnumerator RemoveObject(GameObject gameObject)
    {
        numObjectsSimulating += 1;
        Destroy(gameObject);
        yield return null;
        numObjectsSimulating -= 1;
    }

    // Built-up Simulation - calls primitives

    private Simulation MoveKitten(int kitten, Vector3 destination, float offset, float speed)
    {
        return Simulation.PrimativeSimulation(MoveObject(kittenObjs[kitten], destination, offset, speed));
    }

    private Simulation MoveKitten(int kitten, Vector3 destination)
    {
        return Simulation.PrimativeSimulation(MoveObject(kittenObjs[kitten], destination, 0, moveSpeed));
    }

    private Simulation MoveDoor(bool toOpen)
    {
        Vector3 destination = toOpen ? doorOpenPosition : doorClosedPosition;
        return Simulation.PrimativeSimulation(MoveObject(door, destination, 0, moveSpeed));
    }

    private Simulation SetKittenBehindBuildingMask(int kitten, bool isInBuilding)
    {
        Transform newParent = isInBuilding ? buildingMask : kittenSpots[kitten];
        return Simulation.PrimativeSimulation(ReparentObject(kittenObjs[kitten], newParent));
    }

    private Simulation SetKittenBehindBuilding(int kitten)
    {
        return Simulation.PrimativeSimulation(ReparentAndReorderObject(kittenObjs[kitten], castleAndPlayers, 0));
    }

    private Simulation ScratchKitten(int kitten)
    {
        float scratchAngle = Random.Range(0, 360);
        return Simulation.CompoundSimulation(new Simulation[] { Simulation.PrimativeSimulation(WipeObject(scratchPrefab, kittenBodies[kitten], scratchAngle)), Simulation.PrimativeSimulation(WipeObject(scratchPrefab, kittenBodies[kitten], scratchAngle - 45)) }, false, false);
    }

    private Simulation AttackKittens(int kittenAttacker)
    {
        List<Simulation> simulations = new List<Simulation>();
        int[] damageDealtToKitten = GameController.instance.damageDealtThisTurn;
        for(int i = 0; i < damageDealtToKitten.Length; i++)
        {
            if (damageDealtToKitten[i] > 0)
            {
                simulations.Add(MoveKitten(kittenAttacker, kittenObjs[i].position, attackDistanceOffset, attackSpeed));
                simulations.Add(ScratchKitten(i));
                simulations.Add(ShowChangeInfoText(i, ChangeInfoText.Health, "" + (-damageDealtToKitten[i]), changeInfoFloatDistance, 0));
            }
        }

        return Simulation.CompoundSimulation(simulations.ToArray(), false, false);
    }

    private Simulation ShowChangeInfoText(int kitten, ChangeInfoText changeInfoText, string infoTextPrefix, float offset, int delayNum)
    {
        List<Simulation> simulations = new List<Simulation>();
        simulations.Add(Simulation.PrimativeSimulation(Wait(delayNum * changeInfoTextDelay)));

        GameObject infoTextPrefab;
        if (changeInfoText == ChangeInfoText.Card)
        {
            infoTextPrefab = cardChangeTextPrefab;
        }
        else if (changeInfoText == ChangeInfoText.Currency)
        {
            infoTextPrefab = currencyChangeTextPrefab;
        }
        else if (changeInfoText == ChangeInfoText.Health)
        {
            infoTextPrefab = healthChangeTextPrefab;
        }
        else if (changeInfoText == ChangeInfoText.Points)
        {
            infoTextPrefab = pointsChangeTextPrefab;
        }
        else
        {
            infoTextPrefab = null;
        }
        Transform originalParent = kittenInfoChangeTextObj[kitten];
        Transform fadeObject = Instantiate(infoTextPrefab, originalParent).transform;
        changeInfoTextTransforms.Add(fadeObject);
        fadeObject.GetComponent<TextMeshProUGUI>().text = infoTextPrefix + fadeObject.GetComponent<TextMeshProUGUI>().text;
        fadeObject.SetParent(castleAndPlayers);

        Vector3 startPosition = fadeObject.position - new Vector3(0, offset);
        Vector3 endPosition = fadeObject.position + new Vector3(0, offset);
        fadeObject.position = startPosition;
        fadeObject.GetComponent<CanvasGroup>().alpha = 0;

        simulations.Add(Simulation.CompoundSimulation(new Simulation[] { Simulation.PrimativeSimulation(FadeObjectInAndOut(fadeObject)), Simulation.PrimativeSimulation(MoveObject(fadeObject, endPosition, 0, 2 * offset / (2 * fadeTime + fadeStayTime))) }, true, false));
        simulations.Add(Simulation.PrimativeSimulation(RemoveObject(fadeObject.gameObject)));
        return Simulation.CompoundSimulation(simulations.ToArray(), false, true);
    }

    // Callable Simulations

    public Simulation MoveKittenToInside1ViaDoor(int kitten)
    {
        Simulation moveToDoor = Simulation.CompoundSimulation(new Simulation[] { MoveKitten(kitten, doorClosedPosition), MoveDoor(true) }, true, false);
        Simulation moveToInside1 = Simulation.CompoundSimulation(new Simulation[] { MoveKitten(kitten, insidePosition1), MoveDoor(false) }, true, false);
        return Simulation.CompoundSimulation(new Simulation[] { moveToDoor, Simulation.PrimativeSimulation(Wait(pauseTime)), SetKittenBehindBuildingMask(kitten, true), Simulation.PrimativeSimulation(Wait(pauseTime)), moveToInside1, SetKittenBehindBuilding(kitten) }, false, false);
    }

    public Simulation MoveKittenToInside2ViaDoorThenInsideLeft(int kitten)
    {
        Simulation moveToDoor = Simulation.CompoundSimulation(new Simulation[] { MoveKitten(kitten, doorClosedPosition), MoveDoor(true) }, true, false);
        Simulation moveToInsideLeft = Simulation.CompoundSimulation(new Simulation[] { MoveKitten(kitten, insideLeftPosition), MoveDoor(false) }, true, false);
        return Simulation.CompoundSimulation(new Simulation[] { moveToDoor, Simulation.PrimativeSimulation(Wait(pauseTime)), SetKittenBehindBuildingMask(kitten, true), Simulation.PrimativeSimulation(Wait(pauseTime)), moveToInsideLeft, MoveKitten(kitten, insidePosition2), SetKittenBehindBuilding(kitten) }, false, false);
    }

    public Simulation MoveKittenToInside1ViaInsideLeftThenDoor(int kitten)
    {
        return Simulation.CompoundSimulation(new Simulation[] { SetKittenBehindBuildingMask(kitten, true), MoveKitten(kitten, insideLeftPosition), MoveKitten(kitten, doorClosedPosition), MoveKitten(kitten, insidePosition1), SetKittenBehindBuilding(kitten) }, false, false);
    }

    public Simulation MoveKittenToHomeViaDoor(int kitten)
    {
        Simulation moveToDoor = Simulation.CompoundSimulation(new Simulation[] { MoveKitten(kitten, doorClosedPosition), MoveDoor(true) }, true, false);
        Simulation moveToHome = Simulation.CompoundSimulation(new Simulation[] { MoveKitten(kitten, kittenSpots[kitten].position), MoveDoor(false) }, true, false);
        return Simulation.CompoundSimulation(new Simulation[] { SetKittenBehindBuildingMask(kitten, true), moveToDoor, Simulation.PrimativeSimulation(Wait(pauseTime)), SetKittenBehindBuildingMask(kitten, false), Simulation.PrimativeSimulation(Wait(pauseTime)), moveToHome }, false, false);
    }

    public Simulation MoveKittenToHomeViaInsideLeftThenDoor(int kitten)
    {
        Simulation moveToDoor = Simulation.CompoundSimulation(new Simulation[] { MoveKitten(kitten, doorClosedPosition), MoveDoor(true) }, true, false);
        Simulation moveToHome = Simulation.CompoundSimulation(new Simulation[] { MoveKitten(kitten, kittenSpots[kitten].position), MoveDoor(false) }, true, false);
        return Simulation.CompoundSimulation(new Simulation[] { SetKittenBehindBuildingMask(kitten, true), MoveKitten(kitten, insideLeftPosition), moveToDoor, Simulation.PrimativeSimulation(Wait(pauseTime)), SetKittenBehindBuildingMask(kitten, false), Simulation.PrimativeSimulation(Wait(pauseTime)), moveToHome }, false, false);
    }

    public Simulation AttackKittensFromOutside(int kittenAttacker)
    {
        List<Simulation> simulations = new List<Simulation>();
        simulations.Add(AttackKittens(kittenAttacker));
        simulations.Add(MoveKitten(kittenAttacker, kittenSpots[kittenAttacker].position, 0, attackSpeed));
        return Simulation.CompoundSimulation(simulations.ToArray(), false, false);
    }

    public Simulation AttackKittensFromInside(int kittenAttacker, int insideIndex)
    {
        List<Simulation> simulations = new List<Simulation>();
        simulations.Add(Simulation.PrimativeSimulation(ReorderObject(kittenObjs[kittenAttacker], castleAndPlayers.childCount - 1)));
        simulations.Add(AttackKittens(kittenAttacker));
        simulations.Add(MoveKitten(kittenAttacker, insideIndex == 0 ? insidePosition1 : insidePosition2, 0, attackSpeed));
        simulations.Add(Simulation.PrimativeSimulation(ReorderObject(kittenObjs[kittenAttacker], 0)));
        return Simulation.CompoundSimulation(simulations.ToArray(), false, false);
    }

    public Simulation KillKitten(int kitten)
    {
        int direction = Random.value < 0.5 ? -1 : 1;
        return Simulation.CompoundSimulation(new Simulation[] { Simulation.PrimativeSimulation(RotateObjectBy(kittenObjs[kitten], direction * 90)), Simulation.PrimativeSimulation(SquishObject(kittenObjs[kitten], 0.75f, true)) }, true, false);
    }

    public Simulation ShowKittenHalo(int kitten, bool isVisible)
    {
        return Simulation.PrimativeSimulation(SetObjectActive(kittenHalos[kitten], isVisible));
    }

    public Simulation SetTimeOfDay()
    {
        int position = GameController.instance.currentSiegeHour - 1;
        int numPlayers = GameController.instance.numLivingPlayersAtStartOfSiegeDay;
        float newAngle = (float)(360 * position) / numPlayers;
        return Simulation.PrimativeSimulation(RotateObjectTo(dayNightBackground, newAngle));
    }

    public Simulation ShowKittenStatChanges(int kitten, ChangeInfoText[] changeInfoTexts, int[] infoChanges)
    {
        List<Simulation> simulations = new List<Simulation>();
        for (int i = 0; i < changeInfoTexts.Length; i++)
        {
            simulations.Add(ShowChangeInfoText(kitten, changeInfoTexts[i], "" + infoChanges[i], changeInfoFloatDistance, i));
            //if (i != changeInfoTexts.Length - 1)
            //{
            //    simulations.Add(Wait(changeInfoTextDelay));
            //}
        }
        return Simulation.CompoundSimulation(simulations.ToArray(), true, true);
    }

    public Simulation RollDiceToValues(int[] dice, int[] values)
    {
        List<Simulation> simulations = new List<Simulation>();
        for (int i = 0; i < dice.Length; i++)
        {
            simulations.Add(Simulation.PrimativeSimulation(RollDieToValue(dice[i], values[i])));

        }
        return Simulation.CompoundSimulation(simulations.ToArray(), true, false);
    }

    // Dice primitives: big so has its own section

    private IEnumerator RollDieToValue(int die, int value)
    {
        numObjectsSimulating += 1;
        float randomSpeedMult = 1 + Random.Range(-diceSpeedRandomness, diceSpeedRandomness);
        float rollTime = 0;
        int passes = 0;
        RectTransform diceRollerRect = diceRollerObjs[die].GetComponent<RectTransform>();
        float yAnchorMinInitial = diceRollerRect.anchorMin.y;
        float yAnchorMinPrevious = yAnchorMinInitial;
        float yAnchorMinTarget = -value;
        while (true)
        {
            if (rollTime > diceRollTimeout)
            {
                // Roll shouldn't exceed this time, just set values and break
                diceRollerRect.anchorMin = new Vector2(0, -value);
                diceRollerRect.anchorMax = new Vector2(1, -value + 7);
                break;
            }

            diceRollerRect.anchorMin += new Vector2(0, -Time.deltaTime * diceRollSpeed * randomSpeedMult);
            diceRollerRect.anchorMax += new Vector2(0, -Time.deltaTime * diceRollSpeed * randomSpeedMult);
            if (diceRollerRect.anchorMin.y < -6)
            {
                // Edge case, check for break separately as well
                diceRollerRect.anchorMin += new Vector2(0, 7);
                diceRollerRect.anchorMax += new Vector2(0, 7);
                if (value == 0)
                {
                    if (passes >= numTimesDiceRollPassesValue)
                    {
                        diceRollerRect.anchorMin = new Vector2(0, 0);
                        diceRollerRect.anchorMax = new Vector2(1, 7);
                        break;
                    }
                    else
                    {
                        passes += 1;
                    }
                }
            }
            else if (diceRollerRect.anchorMin.y < yAnchorMinTarget && yAnchorMinPrevious > yAnchorMinTarget)
            {
                if (passes >= numTimesDiceRollPassesValue)
                {
                    diceRollerRect.anchorMin = new Vector2(0, -value);
                    diceRollerRect.anchorMax = new Vector2(1, -value + 7);
                    break;
                }
                else
                {
                    passes += 1;
                }
            }
            yAnchorMinPrevious = diceRollerRect.anchorMin.y;
            rollTime += Time.deltaTime;
            yield return null;
        }
        numObjectsSimulating -= 1;
    }

    public void SetDieObj(int die, int value)
    {
        numObjectsSimulating += 1;
        RectTransform diceRollerRect = diceRollerObjs[die].GetComponent<RectTransform>();
        diceRollerRect.anchorMin = new Vector2(0, -value);
        diceRollerRect.anchorMax = new Vector2(1, -value + 7);
        numObjectsSimulating -= 1;
    }

    public void SetDieVisibleObj(int die, bool isVisible)
    {
        numObjectsSimulating += 1;
        diceObj[die].gameObject.SetActive(isVisible);
        numObjectsSimulating -= 1;
    }

    public void SetDieButtonInteractableObj(int die, bool isInteractable)
    {
        numObjectsSimulating += 1;
        diceObj[die].GetComponent<Button>().interactable = isInteractable;
        diceObj[die].Find("IsActive").gameObject.SetActive(!isInteractable);
        numObjectsSimulating -= 1;
    }

    public void SetDieHeldObj(int die, bool isHeld)
    {
        numObjectsSimulating += 1;
        diceObj[die].Find("IsHeld").gameObject.SetActive(isHeld);
        numObjectsSimulating -= 1;
    }

    public void SetDiceBordersObj(int numDice)
    {
        numObjectsSimulating += 1;
        diceBorders.Find("UpperLeft").GetComponent<RectTransform>().anchorMin = new Vector2(0, 0.5f);
        diceBorders.Find("UpperLeft").GetComponent<RectTransform>().anchorMax = new Vector2(0, 1f);
        diceBorders.Find("UpperLeft").gameObject.SetActive(numDice > 0);

        diceBorders.Find("LowerLeft").GetComponent<RectTransform>().anchorMin = new Vector2(0, 0f);
        diceBorders.Find("LowerLeft").GetComponent<RectTransform>().anchorMax = new Vector2(0, 0.5f);
        diceBorders.Find("LowerLeft").gameObject.SetActive(numDice > 1);

        diceBorders.Find("UpperRight").GetComponent<RectTransform>().anchorMin = new Vector2(0.25f * ((numDice + 1) / 2), 0.5f);
        diceBorders.Find("UpperRight").GetComponent<RectTransform>().anchorMax = new Vector2(0.25f * ((numDice + 1) / 2), 1f);
        diceBorders.Find("UpperRight").gameObject.SetActive(numDice > 0);

        diceBorders.Find("LowerRight").GetComponent<RectTransform>().anchorMin = new Vector2(0.25f * (numDice / 2), 0f);
        diceBorders.Find("LowerRight").GetComponent<RectTransform>().anchorMax = new Vector2(0.25f * (numDice / 2), 0.5f);
        diceBorders.Find("LowerRight").gameObject.SetActive(numDice > 1);

        diceBorders.Find("Top1").gameObject.SetActive(numDice > 0);
        diceBorders.Find("Top2").gameObject.SetActive(numDice > 2);
        diceBorders.Find("Top3").gameObject.SetActive(numDice > 4);
        diceBorders.Find("Top4").gameObject.SetActive(numDice > 6);

        diceBorders.Find("Bottom1").GetComponent<RectTransform>().anchorMin = new Vector2(0, 0.5f * BoolToInt(numDice < 2));
        diceBorders.Find("Bottom1").GetComponent<RectTransform>().anchorMax = new Vector2(0.25f, 0.5f * BoolToInt(numDice < 2));
        diceBorders.Find("Bottom1").gameObject.SetActive(numDice > 0);


        diceBorders.Find("Bottom2").GetComponent<RectTransform>().anchorMin = new Vector2(0.25f, 0.5f * BoolToInt(numDice < 4));
        diceBorders.Find("Bottom2").GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f * BoolToInt(numDice < 4));
        diceBorders.Find("Bottom2").gameObject.SetActive(numDice > 2);

        diceBorders.Find("Bottom3").GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f * BoolToInt(numDice < 6));
        diceBorders.Find("Bottom3").GetComponent<RectTransform>().anchorMax = new Vector2(0.75f, 0.5f * BoolToInt(numDice < 6));
        diceBorders.Find("Bottom3").gameObject.SetActive(numDice > 4);

        diceBorders.Find("Bottom4").GetComponent<RectTransform>().anchorMin = new Vector2(0.75f, 0.5f * BoolToInt(numDice < 8));
        diceBorders.Find("Bottom4").GetComponent<RectTransform>().anchorMax = new Vector2(1, 0.5f * BoolToInt(numDice < 8));
        diceBorders.Find("Bottom4").gameObject.SetActive(numDice > 6);
        numObjectsSimulating -= 1;
    }

    // Functions

    public int Sign(float value)
    {
        return value > 0 ? 1 : (value < 0 ? -1 : 0);
    }

    public int BoolToInt(bool value)
    {
        if (value)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }
}
