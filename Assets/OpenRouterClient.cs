
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class ChatMessage
{
    public string role;
    public string content;

    public ChatMessage(string role, string content)
    {
        this.role = role;
        this.content = content;
    }
}

[Serializable]
public class OpenRouterRequest
{
    public string model;
    public List<ChatMessage> messages;
    public int max_tokens;
    public float temperature;
}

[Serializable]
public class OpenRouterResponse
{
    public List<OpenRouterChoice> choices;
    public OpenRouterError error;
}

[Serializable]
public class OpenRouterChoice
{
    public OpenRouterMessage message;
}

[Serializable]
public class OpenRouterMessage
{
    public string role;
    public string content;
}

[Serializable]
public class OpenRouterError
{
    public string message;
    public string code;
}

public class OpenRouterClient : MonoBehaviour
{
    [Header("OpenRouter")]
    [SerializeField] private string apiKey;
    [SerializeField] private string model = "recursive/flint-v1.0:free";

    [Header("Configuração")]
    [SerializeField] private int maxTokens = 1200;
    [SerializeField] private float temperature = 0.2f;
    [SerializeField] private int timeout = 60;

    private const string endpoint =
        "https://openrouter.ai/api/v1/chat/completions";

    public bool IsConfigured
    {
        get
        {
            return !string.IsNullOrWhiteSpace(apiKey) &&
                   !string.IsNullOrWhiteSpace(model);
        }
    }

    public void SendChat(
        List<ChatMessage> messages,
        Action<string> onSuccess,
        Action<string> onError)
    {
        if (!IsConfigured)
        {
            onError?.Invoke("API Key ou modelo não configurado.");
            return;
        }

        if (messages == null || messages.Count == 0)
        {
            onError?.Invoke("A conversa está vazia.");
            return;
        }

        StartCoroutine(
            SendRequest(messages, onSuccess, onError)
        );
    }

    private IEnumerator SendRequest(
        List<ChatMessage> messages,
        Action<string> onSuccess,
        Action<string> onError)
    {
        OpenRouterRequest body = new OpenRouterRequest
        {
            model = model.Trim(),
            messages = messages,
            max_tokens = maxTokens,
            temperature = temperature
        };

        string json = JsonUtility.ToJson(body);
        byte[] bodyBytes = System.Text.Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request =
               new UnityWebRequest(endpoint, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader(
                "Authorization",
                "Bearer " + apiKey.Trim()
            );

            request.timeout = timeout;

            Debug.Log("[OpenRouter] Enviando mensagem...");
            Debug.Log("[OpenRouter] Modelo: " + model);

            yield return request.SendWebRequest();

            string responseText =
                request.downloadHandler != null
                ? request.downloadHandler.text
                : "";

            if (request.result != UnityWebRequest.Result.Success)
            {
                string error =
                    "Erro HTTP " + request.responseCode +
                    " - " + request.error;

                if (!string.IsNullOrWhiteSpace(responseText))
                    error += "\nResposta: " + responseText;

                Debug.LogError("[OpenRouter] " + error);
                onError?.Invoke(error);
                yield break;
            }

            if (string.IsNullOrWhiteSpace(responseText))
            {
                string error = "O servidor retornou uma resposta vazia.";
                Debug.LogError("[OpenRouter] " + error);
                onError?.Invoke(error);
                yield break;
            }

            OpenRouterResponse response = null;

            try
            {
                response = JsonUtility.FromJson<OpenRouterResponse>(
                    responseText
                );
            }
            catch (Exception exception)
            {
                string error =
                    "Não foi possível interpretar a resposta: " +
                    exception.Message;

                Debug.LogError("[OpenRouter] " + error);
                Debug.LogError(responseText);

                onError?.Invoke(error);
                yield break;
            }

            if (response == null ||
                response.choices == null ||
                response.choices.Count == 0 ||
                response.choices[0].message == null)
            {
                string error =
                    response != null && response.error != null
                    ? response.error.message
                    : "A API não retornou nenhuma mensagem válida.";

                Debug.LogError("[OpenRouter] " + error);
                Debug.LogError(responseText);

                onError?.Invoke(error);
                yield break;
            }

            string answer = response.choices[0].message.content;

            if (string.IsNullOrWhiteSpace(answer))
            {
                string error =
                    "O modelo retornou conteúdo vazio.";

                Debug.LogError("[OpenRouter] " + error);
                Debug.LogError(responseText);

                onError?.Invoke(error);
                yield break;
            }

            Debug.Log("[OpenRouter] Resposta recebida com sucesso!");

            onSuccess?.Invoke(answer.Trim());
        }
    }
}
