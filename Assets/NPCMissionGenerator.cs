
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class NPCMissionData
{
    public string titulo;
    public string objetivo;
    public string recompensa;
}

[Serializable]
public class MissionStringEvent : UnityEvent<string>
{
}

public class NPCMissionGenerator : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private OpenRouterClient client;
    [SerializeField] private NpcCapsuleController npc;

    [Header("Configuração")]
    [SerializeField] private int maxWords = 60;

    [Header("Missão alternativa")]
    [SerializeField] private string fallbackTitle = "Investigar a região";
    [SerializeField]
    private string fallbackObjective =
        "Explore a região indicada e retorne ao NPC.";
    [SerializeField] private string fallbackReward = "25 moedas";

    [Header("Eventos")]
    public MissionStringEvent onMissionGenerated =
        new MissionStringEvent();

    private bool generating;

    private void Awake()
    {
        if (client == null)
            client = GetComponent<OpenRouterClient>();

        if (npc == null)
            npc = GetComponent<NpcCapsuleController>();
    }

    public void GenerateMission()
    {
        if (generating)
        {
            Debug.LogWarning("Uma missão já está sendo gerada.");
            return;
        }

        if (client == null || !client.IsConfigured)
        {
            Debug.LogError(
                "NPCMissionGenerator: OpenRouterClient não configurado."
            );
            return;
        }

        generating = true;

        List<ChatMessage> messages = new List<ChatMessage>
        {
            new ChatMessage(
                "system",
                "Você é um gerador de missões para um jogo. " +
                "Crie uma missão curta, simples e adequada ao universo do NPC. " +
                "Responda exclusivamente com JSON válido, sem Markdown, " +
                "sem explicações e sem texto fora do JSON. " +
                "O JSON deve conter os campos titulo, objetivo e recompensa. " +
                "Todos os campos devem ser strings."
            ),

            new ChatMessage(
                "user",
                "Gere uma missão original para o jogador. " +
                "O título deve ser curto. O objetivo deve explicar claramente " +
                "o que o jogador precisa fazer. A recompensa deve ser simples. " +
                "Respeite o limite de " + maxWords + " palavras no total. " +
                "Formato: " +
                "{\"titulo\":\"...\",\"objetivo\":\"...\",\"recompensa\":\"...\"}"
            )
        };

        client.SendChat(
            messages,
            OnMissionReceived,
            OnMissionError
        );
    }

    private void OnMissionReceived(string response)
    {
        generating = false;

        if (!TryParseMission(response, out NPCMissionData mission))
        {
            Debug.LogError(
                "O LLM retornou uma missão inválida. Resposta: " + response
            );
            return;
        }

        string formattedMission =
            "NOVA MISSÃO\n" +
            "Título: " + mission.titulo + "\n" +
            "Objetivo: " + mission.objetivo + "\n" +
            "Recompensa: " + mission.recompensa;

        Debug.Log(formattedMission);

        onMissionGenerated?.Invoke(formattedMission);

        if (npc != null)
        {
            npc.ApplyIntent("missao");
        }
        else
        {
            Debug.LogWarning(
                "NPCMissionGenerator: NpcCapsuleController não encontrado."
            );
        }
    }

    private void OnMissionError(string error)
    {
        generating = false;

        Debug.LogError("Erro ao gerar missão: " + error);

        NPCMissionData fallback = new NPCMissionData
        {
            titulo = fallbackTitle,
            objetivo = fallbackObjective,
            recompensa = fallbackReward
        };

        string formattedMission =
            "NOVA MISSÃO (ALTERNATIVA)\n" +
            "Título: " + fallback.titulo + "\n" +
            "Objetivo: " + fallback.objetivo + "\n" +
            "Recompensa: " + fallback.recompensa;

        Debug.Log(formattedMission);

        onMissionGenerated?.Invoke(formattedMission);
    }

    private bool TryParseMission(
        string response,
        out NPCMissionData mission)
    {
        mission = null;

        if (string.IsNullOrWhiteSpace(response))
            return false;

        int start = response.IndexOf('{');
        int end = response.LastIndexOf('}');

        if (start < 0 || end <= start)
            return false;

        string json = response.Substring(
            start,
            end - start + 1
        );

        try
        {
            NPCMissionData parsed =
                JsonUtility.FromJson<NPCMissionData>(json);

            if (parsed == null)
                return false;

            parsed.titulo = parsed.titulo?.Trim();
            parsed.objetivo = parsed.objetivo?.Trim();
            parsed.recompensa = parsed.recompensa?.Trim();

            if (string.IsNullOrWhiteSpace(parsed.titulo) ||
                string.IsNullOrWhiteSpace(parsed.objetivo) ||
                string.IsNullOrWhiteSpace(parsed.recompensa))
            {
                return false;
            }

            int totalWords =
                CountWords(parsed.titulo) +
                CountWords(parsed.objetivo) +
                CountWords(parsed.recompensa);

            if (totalWords > maxWords)
            {
                Debug.LogWarning(
                    "Missão rejeitada: ultrapassou o limite de palavras."
                );
                return false;
            }

            mission = parsed;
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Erro ao interpretar JSON da missão: " +
                exception.Message
            );
            return false;
        }
    }

    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text.Split(
            new char[] { ' ', '\n', '\r', '\t' },
            StringSplitOptions.RemoveEmptyEntries
        ).Length;
    }
}
