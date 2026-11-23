
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

[RequireComponent(typeof(NavMeshAgent))]
public class NpcCapsuleController : MonoBehaviour
{
    public enum NPCIntent
    {
        neutro,
        alerta,
        missao
    }

    private enum State
    {
        Patrolling,
        Chasing,
        Talking,
        Idle,
        Mission
    }

    [Header("Referências")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private NpcDialogue dialogue;

    [Header("Destino da missão")]
    [SerializeField] private Transform missionDestination;

    [Header("Movimento")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 8f;
    [SerializeField] private float followStoppingDistance = 1.5f;

    [Header("Patrulha")]
    [SerializeField] private float waypointTolerance = 0.3f;
    [SerializeField] private float waitTimeAtPoint = 1.5f;

    [Header("Detecção")]
    [SerializeField] private float chaseRadius = 6f;
    [SerializeField] private float stopChaseRadius = 9f;

    [Header("Conversa")]
    [SerializeField] private float talkRadius = 1.8f;
    [SerializeField] private float conversationCooldown = 15f;

    [Header("Feedback visual")]
    [SerializeField] private Renderer npcRenderer;
    [SerializeField] private Color neutralColor = Color.white;
    [SerializeField] private Color alertColor = Color.red;
    [SerializeField] private Color missionColor = Color.green;

    [Header("Eventos")]
    [SerializeField] private UnityEvent<string> onIntentChanged;

    private NavMeshAgent agent;
    private Material npcMaterial;

    private int currentPatrolIndex;

    private float waitTimer;
    private float conversationCooldownTimer;

    private bool hasGreetedThisEncounter;
    private bool missionCompleted;
    private bool hasRequestedDestination;

    private Vector3 lastDestination;

    private State currentState = State.Patrolling;
    private NPCIntent currentIntent = NPCIntent.neutro;

    public NPCIntent CurrentIntent => currentIntent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }

        if (dialogue == null)
            dialogue = GetComponent<NpcDialogue>();

        if (npcRenderer != null)
            npcMaterial = npcRenderer.material;

        agent.speed = moveSpeed;
        agent.angularSpeed = rotationSpeed * 90f;
        agent.stoppingDistance = followStoppingDistance;
        agent.autoBraking = true;
        agent.updateRotation = true;
    }

    private void Start()
    {
        UpdateVisual();
        onIntentChanged?.Invoke(currentIntent.ToString());

        if (!agent.isOnNavMesh)
        {
            Debug.LogError(
                "NPC não está sobre o NavMesh. " +
                "Verifique se a superfície foi calculada."
            );
        }
    }

    private void Update()
    {
        if (agent == null || !agent.isOnNavMesh)
            return;

        if (conversationCooldownTimer > 0f)
            conversationCooldownTimer -= Time.deltaTime;

        switch (currentIntent)
        {
            case NPCIntent.neutro:
                UpdateNormalBehavior();
                break;

            case NPCIntent.alerta:
                UpdateAlert();
                break;

            case NPCIntent.missao:
                UpdateMission();
                break;
        }
    }

    public void ApplyIntent(string intent)
    {
        if (!System.Enum.TryParse(
            intent,
            true,
            out NPCIntent parsed))
        {
            Debug.LogWarning("Intenção desconhecida: " + intent);
            return;
        }

        if (!System.Enum.IsDefined(typeof(NPCIntent), parsed))
            return;

        if (parsed == NPCIntent.missao &&
            missionDestination == null)
        {
            Debug.LogWarning(
                "Não foi possível iniciar a missão: " +
                "Mission Destination não está configurado no Inspector."
            );
            return;
        }

        if (currentIntent == parsed)
            return;

        currentIntent = parsed;
        hasRequestedDestination = false;

        switch (parsed)
        {
            case NPCIntent.neutro:
                currentState = State.Idle;
                waitTimer = 0f;
                missionCompleted = false;
                break;

            case NPCIntent.alerta:
                currentState = State.Chasing;
                break;

            case NPCIntent.missao:
                currentState = State.Mission;
                missionCompleted = false;
                break;
        }

        UpdateVisual();
        onIntentChanged?.Invoke(parsed.ToString());

        Debug.Log("Nova intenção do NPC: " + parsed);
    }

    private void UpdateNormalBehavior()
    {
        UpdateState();

        switch (currentState)
        {
            case State.Patrolling:
                Patrol();
                break;

            case State.Chasing:
                Chase();
                break;

            case State.Talking:
                StopAgent();
                FacePlayer();
                break;

            case State.Idle:
                StopAgent();

                if (patrolPoints != null &&
                    patrolPoints.Length > 0)
                {
                    currentState = State.Patrolling;
                }
                break;
        }
    }

    private void UpdateState()
    {
        if (player == null)
        {
            currentState =
                patrolPoints != null &&
                patrolPoints.Length > 0
                    ? State.Patrolling
                    : State.Idle;

            return;
        }

        float distance = Vector3.Distance(
            transform.position,
            player.position
        );

        if (distance <= talkRadius)
        {
            currentState = State.Talking;
            StopAgent();

            if (!hasGreetedThisEncounter &&
                conversationCooldownTimer <= 0f &&
                dialogue != null)
            {
                dialogue.StartConversation();
                hasGreetedThisEncounter = true;
                conversationCooldownTimer = conversationCooldown;
            }

            return;
        }

        if (distance > talkRadius + 0.5f)
            hasGreetedThisEncounter = false;

        if (currentState == State.Chasing)
        {
            if (distance > stopChaseRadius)
            {
                currentState =
                    patrolPoints != null &&
                    patrolPoints.Length > 0
                        ? State.Patrolling
                        : State.Idle;

                hasRequestedDestination = false;
            }

            return;
        }

        if (distance <= chaseRadius)
        {
            currentState = State.Chasing;
            return;
        }

        if (currentState == State.Talking)
        {
            currentState =
                patrolPoints != null &&
                patrolPoints.Length > 0
                    ? State.Patrolling
                    : State.Idle;
        }
    }

    // ALERTA: continua seguindo o jogador.
    private void UpdateAlert()
    {
        if (player == null)
        {
            StopAgent();
            return;
        }

        agent.speed = moveSpeed;
        agent.stoppingDistance = followStoppingDistance;

        MoveTo(player.position);
    }

    private void UpdateMission()
    {
        if (missionCompleted)
        {
            StopAgent();
            currentState = State.Idle;
            return;
        }

        if (missionDestination == null)
        {
            Debug.LogWarning("Mission Destination não está configurado.");
            ApplyIntent("neutro");
            return;
        }

        currentState = State.Mission;
        agent.stoppingDistance = waypointTolerance;

        MoveTo(missionDestination.position);

        if (!agent.pathPending &&
            agent.hasPath &&
            agent.remainingDistance <= waypointTolerance + 0.1f)
        {
            missionCompleted = true;
            currentState = State.Idle;
            StopAgent();

            Debug.Log("NPC chegou ao destino da missão.");
        }

        if (!agent.pathPending &&
            agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            Debug.LogWarning(
                "O destino da missão não possui um caminho válido."
            );

            ApplyIntent("neutro");
        }
    }

    private void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            currentState = State.Idle;
            StopAgent();
            return;
        }

        if (currentPatrolIndex >= patrolPoints.Length)
            currentPatrolIndex = 0;

        Transform target = patrolPoints[currentPatrolIndex];

        if (target == null)
        {
            currentPatrolIndex =
                (currentPatrolIndex + 1) % patrolPoints.Length;

            hasRequestedDestination = false;
            return;
        }

        agent.stoppingDistance = waypointTolerance;
        MoveTo(target.position);

        if (!agent.pathPending &&
            agent.remainingDistance <= waypointTolerance + 0.1f)
        {
            StopAgent();
            waitTimer += Time.deltaTime;

            if (waitTimer >= waitTimeAtPoint)
            {
                waitTimer = 0f;

                currentPatrolIndex =
                    (currentPatrolIndex + 1) % patrolPoints.Length;

                hasRequestedDestination = false;
            }
        }
    }

    private void Chase()
    {
        if (player == null)
        {
            StopAgent();
            return;
        }

        agent.speed = moveSpeed;
        agent.stoppingDistance = followStoppingDistance;

        MoveTo(player.position);
    }

    private void MoveTo(Vector3 destination)
    {
        if (!agent.isOnNavMesh)
            return;

        agent.isStopped = false;

        if (!hasRequestedDestination ||
            Vector3.Distance(lastDestination, destination) > 0.5f)
        {
            bool accepted = agent.SetDestination(destination);

            if (accepted)
            {
                lastDestination = destination;
                hasRequestedDestination = true;
            }
        }
    }

    private void StopAgent()
    {
        if (agent == null || !agent.isOnNavMesh)
            return;

        agent.isStopped = true;
        agent.ResetPath();
        hasRequestedDestination = false;
    }

    private void FacePlayer()
    {
        if (player == null)
            return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction.normalized);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void UpdateVisual()
    {
        if (npcMaterial == null)
            return;

        switch (currentIntent)
        {
            case NPCIntent.neutro:
                npcMaterial.color = neutralColor;
                break;

            case NPCIntent.alerta:
                npcMaterial.color = alertColor;
                break;

            case NPCIntent.missao:
                npcMaterial.color = missionColor;
                break;
        }
    }

    public void SetPlayer(Transform target)
    {
        player = target;
    }

    public void SetMissionDestination(Transform destination)
    {
        missionDestination = destination;
        hasRequestedDestination = false;
        missionCompleted = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopChaseRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, talkRadius);

        if (missionDestination != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, missionDestination.position);
            Gizmos.DrawWireSphere(missionDestination.position, 0.3f);
        }
    }
}
