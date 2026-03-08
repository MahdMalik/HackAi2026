using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Actuators;

/// <summary>
/// Initialize Scene with Agents
/// 
/// Usage: Create empty GameObject, attach this script, configure parameters, then play.
/// This will auto-generate all required agents and setup.
/// </summary>
public class SceneInitializer : MonoBehaviour
{
    [Header("Agent Counts")]
    [SerializeField] private int killerAgentCount = 3;
    [SerializeField] private int survivorAgentCount = 3;
    
    [Header("Spawn Settings")]
    [SerializeField] private float spawnRadius = 15f;
    [SerializeField] private Vector3 spawnCenter = Vector3.zero;
    
    [Header("RL Decision Settings")]
    [SerializeField] private float decisionIntervalSeconds = 3f;
    
    [Header("Prefabs")]
    [SerializeField] private GameObject killerPrefab;
    [SerializeField] private GameObject survivorPrefab;
    
    private GameObject evilFellasParent;
    private GameObject normalFellasParent;
    private GameObject gameManagerObject;
    
    void Start()
    {
        InitializeScene();
    }
    
    public void InitializeScene()
    {
        Debug.Log("Initializing ModelMystery Scene...");
        
        // Ensure there is a visible camera & light so agents actually show up
        EnsureCameraAndLight();
        
        // Create GameManager
        CreateGameManager();
        
        // Create parent containers
        evilFellasParent = CreateParent("EvilFellas");
        normalFellasParent = CreateParent("NormalFellas");
        
        // Spawn agents
        for (int i = 0; i < killerAgentCount; i++)
        {
            CreateKillerAgent(i, evilFellasParent);
        }
        
        for (int i = 0; i < survivorAgentCount; i++)
        {
            CreateSurvivorAgent(i, normalFellasParent);
        }
        
        // Initialize robots in GameManager after spawning
        GameManager gm = gameManagerObject.GetComponent<GameManager>();
        if (gm != null)
        {
            gm.InitializeRobots();
        }
        
        Debug.Log($"Scene initialized: {killerAgentCount} Killers + {survivorAgentCount} Survivors");
    }
    
    private void CreateGameManager()
    {
        GameObject existing = GameObject.Find("GameManager");
        if (existing != null)
        {
            Debug.LogWarning("GameManager already exists! Using existing one.");
            gameManagerObject = existing;
            return;
        }
        
        gameManagerObject = new GameObject("GameManager");
        GameManager gm = gameManagerObject.AddComponent<GameManager>();
        gm.matchTime = 30;  // 30 seconds per match
        
        // Add MatchTimerUI for display
        gameManagerObject.AddComponent<MatchTimerUI>();
    }
    
    private GameObject CreateParent(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null)
        {
            Debug.LogWarning($"{name} already exists!");
            return existing;
        }
        
        GameObject parent = new GameObject(name);
        return parent;
    }
    
    private void CreateKillerAgent(int index, GameObject parent)
    {
    string name = $"KillerAgent_{index}";
    GameObject agent = killerPrefab != null 
        ? Instantiate(killerPrefab, parent.transform) 
        : new GameObject(name);
    
    agent.name = name;
    agent.transform.parent = parent.transform;
    agent.transform.position = GetRandomSpawnPosition();
    
    // Add visual representation (sphere)
    SetupAgentVisuals(agent, Color.red); // Red for killers
    
    // ✅ FIRST: Setup BehaviorParameters BEFORE agent scripts initialize
    SetupBehaviorParameters(agent, "KillerAgent", new int[] { 5, 5, 5 }, Constants.KILLER_OBS_SIZE);

    if(agent == null)
    {
        Debug.Log("Ok that's real bad.");
    }

    // THEN: Add action/movement scripts
    if (agent.GetComponent<EvilActionScript>() == null)
        agent.AddComponent<EvilActionScript>();
    
    EvilActionScript actionScript = agent.GetComponent<EvilActionScript>();
    actionScript.manager = gameManagerObject;
        
    // LAST: Add Agent script (Initialize() fires here — BehaviorParameters already correct)
    if (agent.GetComponent<EvilAgentScript>() == null)
        agent.AddComponent<EvilAgentScript>();
        
    if (agent.GetComponent<Rigidbody2D>() == null)
    {
        Rigidbody2D rb = agent.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }
    
    Debug.Log($"Created {name}");
    }

    private void CreateSurvivorAgent(int index, GameObject parent)
    {
    string name = $"SurvivorAgent_{index}";
    GameObject agent = survivorPrefab != null 
        ? Instantiate(survivorPrefab, parent.transform) 
        : new GameObject(name);
    
    agent.name = name;
    agent.transform.parent = parent.transform;
    agent.transform.position = GetRandomSpawnPosition();

    // Add visual representation (sphere)
    SetupAgentVisuals(agent, Color.cyan); // Cyan for survivors
    
    // ✅ FIRST: Setup BehaviorParameters BEFORE agent scripts initialize
    SetupBehaviorParameters(agent, "SurvivorAgent", new int[] { 4, 3, 3 }, Constants.SURVIVOR_OBS_SIZE);

    if (agent.GetComponent<NormalActionScript>() == null)
        agent.AddComponent<NormalActionScript>();
    
    NormalActionScript actionScript = agent.GetComponent<NormalActionScript>();
    actionScript.manager = gameManagerObject;
        
    // LAST: Add Agent script
    if (agent.GetComponent<NormalAgentScript>() == null)
        agent.AddComponent<NormalAgentScript>();
        
    if (agent.GetComponent<Rigidbody2D>() == null)
    {
        Rigidbody2D rb = agent.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }
    
    Debug.Log($"Created {name}");
    }
    
    private void SetupBehaviorParameters(GameObject agentGO, string behaviorName, 
        int[] discreteActionSizes, int observationSize)
    {
        BehaviorParameters bp = agentGO.GetComponent<BehaviorParameters>();
        if (bp == null)
            bp = agentGO.AddComponent<BehaviorParameters>();
        
        // configure brain parameters programmatically so the prefab doesn't need manual setup
        bp.BehaviorName = behaviorName;
        bp.BehaviorType = BehaviorType.Default;
        bp.BrainParameters.VectorObservationSize = observationSize;
        bp.BrainParameters.NumStackedVectorObservations = 1;
        bp.BrainParameters.ActionSpec = ActionSpec.MakeDiscrete(discreteActionSizes);
        
        // ensure the agent automatically requests decisions on a fixed interval
        DecisionRequester dr = agentGO.GetComponent<DecisionRequester>();
        if (dr == null)
            dr = agentGO.AddComponent<DecisionRequester>();
        
        // Convert the desired interval in seconds to fixed-update steps
        int period = 1;
        if (decisionIntervalSeconds > 0f)
        {
            float fixedDt = Time.fixedDeltaTime > 0f ? Time.fixedDeltaTime : 0.02f;
            period = Mathf.Max(1, Mathf.RoundToInt(decisionIntervalSeconds / fixedDt));
        }
        dr.DecisionPeriod = period;
        dr.TakeActionsBetweenDecisions = true;
        
        Debug.Log($"Setup {behaviorName}: Obs={observationSize}, Actions=[{string.Join(",", discreteActionSizes)}], DecisionPeriod={dr.DecisionPeriod} (≈{decisionIntervalSeconds:F2}s) and added DecisionRequester");
    }
    
    private void SetupAgentVisuals(GameObject agent, Color agentColor)
    {
        // Remove any existing 3D mesh components so we can render as a 2D sprite
        var existingMeshFilter = agent.GetComponent<MeshFilter>();
        if (existingMeshFilter != null)
            DestroyImmediate(existingMeshFilter);
        var existingMeshRenderer = agent.GetComponent<MeshRenderer>();
        if (existingMeshRenderer != null)
            DestroyImmediate(existingMeshRenderer);

        // Ensure there is a SpriteRenderer on the root agent object
        SpriteRenderer sr = agent.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = agent.AddComponent<SpriteRenderer>();

        // Create a simple 1x1 white texture and turn it into a square sprite
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();

        Rect rect = new Rect(0, 0, 1, 1);
        Vector2 pivot = new Vector2(0.5f, 0.5f);
        Sprite sprite = Sprite.Create(tex, rect, pivot, 100f);

        sr.sprite = sprite;
        sr.color = agentColor;

        // Scale the agent so the sprite is a reasonable size on screen
        agent.transform.localScale = new Vector3(100f, 100f, 100f);

        // Add a 2D collider so collisions can drive kill logic
        CircleCollider2D col = agent.GetComponent<CircleCollider2D>();
        if (col == null)
        {
            col = agent.AddComponent<CircleCollider2D>();
        }
        col.isTrigger = false;
        col.radius = 0.25f;
    }
    
    private Vector3 GetRandomSpawnPosition()
    {
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float randomDistance = Random.Range(0f, spawnRadius);
        
        float x = spawnCenter.x + Mathf.Cos(randomAngle) * randomDistance;
        float y = spawnCenter.y + Mathf.Sin(randomAngle) * randomDistance;
        
        return new Vector3(x, y, 0);
    }
    
    /// <summary>
    /// Ensure there is a MainCamera and some basic lighting so the
    /// auto‑spawned agents are actually visible on screen.
    /// </summary>
    private void EnsureCameraAndLight()
    {
        // Camera
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camGO = new GameObject("Main Camera");
            mainCam = camGO.AddComponent<Camera>();
            camGO.tag = "MainCamera";
        }

        mainCam.orthographic = true;
        mainCam.transform.position = new Vector3(spawnCenter.x, spawnCenter.y, -10f);
        mainCam.transform.rotation = Quaternion.identity;

        // Size so the whole spawn radius is comfortably inside view
        float desiredSize = Mathf.Max(spawnRadius * 0.8f, 10f);
        mainCam.orthographicSize = desiredSize;

        // Simple directional light if none exists (helps 3D cubes be visible)
        if (FindFirstObjectByType<Light>() == null)
        {
            GameObject lightGO = new GameObject("Directional Light");
            Light light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 1.0f;
            lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
    }
    
    [ContextMenu("Initialize Scene")]
    public void QuickInitialize()
    {
        InitializeScene();
    }
    
    [ContextMenu("Clear Scene Agents")]
    public void ClearScene()
    {
        GameObject evil = GameObject.Find("EvilFellas");
        if (evil != null) DestroyImmediate(evil);
        
        GameObject normal = GameObject.Find("NormalFellas");
        if (normal != null) DestroyImmediate(normal);
        
        Debug.Log("Scene cleared");
    }
}
