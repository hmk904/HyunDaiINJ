﻿using HyunDaiINJ.DATA.DTO;
using MQTTnet.Client;
using MQTTnet;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json;
using System.Text;
using System.Linq;

public class MQTTModel
{
    private IMqttClient? mqttClient;

    // Vision 관련 메시지 이벤트
    public event Action<string, MqttVisionDTO>? VisionMessageReceived;

    // PLC 관련 메시지 이벤트
    public event Action<string, MqttProcessDTO>? ProcessMessageReceived;

    public bool MqttConnected => mqttClient?.IsConnected ?? false;

    public async Task MqttConnect()
    {
        var factory = new MqttFactory();
        mqttClient = factory.CreateMqttClient();

        mqttClient.ApplicationMessageReceivedAsync += MqttRecivedMessage;

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("43.203.159.137", 1883)
            .WithCredentials("admin", "vapor")
            .WithCleanSession(true)
            .Build();

        try
        {
            await mqttClient.ConnectAsync(options);
            Console.WriteLine(mqttClient.IsConnected
                ? "[MqttService] MQTT 연결 성공"
                : "[MqttService] MQTT 연결 실패");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MQTT 연결 실패: {ex.Message}");
        }
    }

    public async Task SubscribeToTopics()
    {
        if (mqttClient?.IsConnected == true)
        {
            // Vision 관련 토픽 구독
            await SubscribeMQTT("Vision/ng/#");

            // PLC 관련 토픽 구독
            await SubscribeMQTT("Process/PLC/#");

            Console.WriteLine("[MqttService] 모든 토픽 구독 완료");
        }
        else
        {
            Console.WriteLine("[MqttService] MQTT 연결이 활성화되지 않았습니다.");
        }
    }

    public async Task SubscribeMQTT(string topic)
    {
        if (mqttClient?.IsConnected == true)
        {
            await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).Build());
            Console.WriteLine($"[MqttService] 토픽 구독: {topic}");
        }
    }

    private async Task MqttRecivedMessage(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic;
        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment.ToArray());

        Console.WriteLine($"[MqttService] 수신된 메시지 - 토픽: {topic}, 데이터: {payload}");

        try
        {
            if (topic.StartsWith("Vision/ng"))
            {
                var visionMessage = JsonConvert.DeserializeObject<MqttVisionDTO>(payload);
                if (visionMessage != null)
                {
                    VisionMessageReceived?.Invoke(topic, visionMessage);
                    Console.WriteLine($"[MqttService] Vision 메시지 처리 완료: {JsonConvert.SerializeObject(visionMessage)}");
                }
            }
            else if (topic.StartsWith("Process/PLC"))
            {
                var processMessage = JsonConvert.DeserializeObject<MqttProcessDTO>(payload);
                if (processMessage != null)
                {
                    ProcessMessageReceived?.Invoke(topic, processMessage);
                    Console.WriteLine($"[MqttService] Process 메시지 처리 완료: {JsonConvert.SerializeObject(processMessage)}");
                }
            }
            else
            {
                Console.WriteLine($"[MqttService] 알 수 없는 토픽: {topic}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MqttService] 메시지 처리 실패: {ex.Message}");
        }
    }

}
