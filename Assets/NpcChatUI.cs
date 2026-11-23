
using UnityEngine;
using UnityEngine.UIElements;

public class NpcChatUI : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private NpcDialogue dialogue;
    [SerializeField] private NPCMissionGenerator missionGenerator;

    private VisualElement root;
    private VisualElement chatLog;
    private ScrollView chatScroll;
    private TextField chatInput;
    private Button sendButton;

    private bool requestPending;
    private string lastNpcMessage = "";

    private void Awake()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (dialogue == null)
            dialogue = FindFirstObjectByType<NpcDialogue>();

        if (missionGenerator == null)
            missionGenerator = FindFirstObjectByType<NPCMissionGenerator>();

        if (uiDocument == null)
        {
            Debug.LogError("NpcChatUI: UIDocument não encontrado.");
            return;
        }

        root = uiDocument.rootVisualElement;

        chatLog = root.Q<VisualElement>("chat-log");
        chatScroll = root.Q<ScrollView>("chat-scroll");
        chatInput = root.Q<TextField>("chat-input");
        sendButton = root.Q<Button>("chat-send-button");

        if (chatLog == null ||
            chatScroll == null ||
            chatInput == null ||
            sendButton == null)
        {
            Debug.LogError(
                "NpcChatUI: Elementos não encontrados no UXML. " +
                "Verifique os nomes chat-log, chat-scroll, " +
                "chat-input e chat-send-button."
            );

            return;
        }

        sendButton.clicked += OnSendClicked;
        chatInput.RegisterCallback<KeyDownEvent>(OnInputKeyDown);

        if (dialogue != null)
            dialogue.onReply.AddListener(OnNpcReply);

        if (missionGenerator != null)
        {
            missionGenerator.onMissionGenerated.AddListener(
                OnMissionGenerated
            );
        }
    }

    private void OnDestroy()
    {
        if (sendButton != null)
            sendButton.clicked -= OnSendClicked;

        if (chatInput != null)
        {
            chatInput.UnregisterCallback<KeyDownEvent>(
                OnInputKeyDown
            );
        }

        if (dialogue != null)
            dialogue.onReply.RemoveListener(OnNpcReply);

        if (missionGenerator != null)
        {
            missionGenerator.onMissionGenerated.RemoveListener(
                OnMissionGenerated
            );
        }
    }

    private void OnInputKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.Return ||
            evt.keyCode == KeyCode.KeypadEnter)
        {
            evt.StopPropagation();
            OnSendClicked();
        }
    }

    private void OnSendClicked()
    {
        if (chatInput == null || requestPending)
            return;

        string message = chatInput.value.Trim();

        if (string.IsNullOrWhiteSpace(message))
            return;

        chatInput.value = string.Empty;

        AddMessage("Você: " + message, true);

        requestPending = true;
        lastNpcMessage = "";

        if (IsMissionCommand(message))
        {
            if (missionGenerator == null)
            {
                requestPending = false;

                AddMessage(
                    "NPC: Não consigo gerar uma missão no momento.",
                    false
                );

                Debug.LogError(
                    "NpcChatUI: NPCMissionGenerator não configurado."
                );

                return;
            }

            AddMessage(
                "NPC: Vou preparar uma tarefa para você...",
                false
            );

            missionGenerator.GenerateMission();
            return;
        }

        if (dialogue != null)
        {
            dialogue.Ask(message);
        }
        else
        {
            requestPending = false;

            Debug.LogError(
                "NpcChatUI: NpcDialogue não encontrado."
            );
        }
    }

    private bool IsMissionCommand(string message)
    {
        string normalized = message.ToLowerInvariant().Trim();

        return normalized.Contains("tarefa") ||
               normalized.Contains("missão") ||
               normalized.Contains("missao");
    }

    private void OnNpcReply(string reply)
    {
        requestPending = false;

        if (string.IsNullOrWhiteSpace(reply))
            return;

        if (reply.Trim() == lastNpcMessage)
            return;

        lastNpcMessage = reply.Trim();

        AddMessage("NPC: " + reply, false);
    }

    private void OnMissionGenerated(string mission)
    {
        requestPending = false;

        if (string.IsNullOrWhiteSpace(mission))
            return;

        if (mission.Trim() == lastNpcMessage)
            return;

        lastNpcMessage = mission.Trim();

        AddMessage("NPC:\n" + mission, false);
    }

    private void AddMessage(string message, bool isPlayer)
    {
        if (chatLog == null)
            return;

        Label messageLabel = new Label(message);

        messageLabel.AddToClassList("chat-message");

        if (isPlayer)
            messageLabel.AddToClassList("player-message");
        else
            messageLabel.AddToClassList("npc-message");

        chatLog.Add(messageLabel);

        if (chatScroll != null)
            chatScroll.ScrollTo(messageLabel);
    }
}
