﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;
using HyunDaiINJ.DATA.DTO;

namespace HyunDaiINJ.Views.Monitoring.Controls.Vision
{
    public partial class VisionWeek : UserControl
    {
        public VisionWeek()
        {
            InitializeComponent();

            // 디자인 모드일 경우, 런타임 전용 로직은 건너뜀
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }
        }

        /// <summary>
        /// 부모(Page 등)에서 주간 데이터(List<VisionNgDTO>)를 받아와
        /// 즉시 차트를 렌더하는 메서드
        /// </summary>
        public async void SetData(List<VisionNgDTO> weekDataList)
        {
            // (1) WebView2가 초기화되지 않았다면, 우선 EnsureCoreWebView2Async() 실행
            if (WebView.CoreWebView2 == null)
            {
                await WebView.EnsureCoreWebView2Async();
            }

            // (2) Chart.js용 config 생성
            var chartConfig = CreateChartConfig(weekDataList);

            // (3) 직렬화
            string configJson = JsonSerializer.Serialize(chartConfig);

            // (4) HTML 생성 후 NavigateToString
            string html = GenerateHtmlContent(configJson);
            WebView.NavigateToString(html);
        }

        #region (Optional) 실시간/서버 로직
        /// <summary>
        /// 실시간 업데이트(예: MQTT 등) 필요하면 추가.
        /// 처음 DB로딩은 Parent에서, 이 메서드는 나중에만 사용 가능
        /// </summary>
        public async Task ListenToServerAsync()
        {
            // 예: 서버에서 주간 데이터 수신 → SetData(newData) ...
        }
        #endregion

        #region Chart.js Config 생성
        /// <summary>
        /// 주간 데이터를 Chart.js에서 사용할 config 형태로 변환
        /// (기존 GenerateChartScript 로직 간소화)
        /// </summary>
        private object CreateChartConfig(List<VisionNgDTO> weekData)
        {
            // 단순 예: X축 1개(“이번주”), ng_label별 dataset
            // 혹은 WeekNumber별로 X축을 구성하는 식도 가능

            var xLabels = new[] { "이번주" };
            var datasets = new List<object>();

            var colorPalette = new List<string>
            {
                "rgba(75, 192, 192, 0.7)",
                "rgba(255, 99, 132, 0.7)",
                "rgba(54, 162, 235, 0.7)",
                "rgba(255, 206, 86, 0.7)",
                "rgba(153, 102, 255, 0.7)",
                "rgba(255, 159, 64, 0.7)"
            };

            int colorIndex = 0;
            foreach (var item in weekData)
            {
                // item.NgLabel / item.LabelCount
                datasets.Add(new
                {
                    label = item.NgLabel,
                    data = new[] { item.LabelCount },
                    backgroundColor = colorPalette[colorIndex % colorPalette.Count]
                });
                colorIndex++;
            }

            var config = new
            {
                type = "bar",
                data = new
                {
                    labels = xLabels,
                    datasets = datasets
                },
                options = new
                {
                    responsive = true,
                    plugins = new
                    {
                        legend = new { position = "top" },
                        title = new
                        {
                            display = true,
                            text = "주간 불량"
                        }
                    }
                }
            };
            return config;
        }
        #endregion

        #region WebView2 / HTML 생성
        private string GenerateHtmlContent(string script)
        {
            var html = $@"
                    <!DOCTYPE html>
                    <html lang='en'>
                    <head>
                        <meta charset='UTF-8'>
                        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                        <script src='https://cdn.jsdelivr.net/npm/chart.js'></script>
                        <style>
                            html, body {{
                                margin: 0;
                                padding: 0;
                                display: flex;
                                align-items: center;
                                justify-content: center;
                                width: 100vw;
                                height: 100vh;
                                background-color: #F4F7FB;
                            }}
                            canvas {{
                                display: block;
                                width: 90vw;
                                height: 80vh;
                            }}
                        </style>
                    </head>
                    <body>
                        <canvas id='stackedBarChart'></canvas>
                        <script>
                            let myChart;
                            function updateChartData(config) {{
                                if (myChart) {{
                                    myChart.data = config.data;
                                    myChart.update('none');
                                }} else {{
                                    const ctx = document.getElementById('stackedBarChart').getContext('2d');
                                    myChart = new Chart(ctx, config);
                                }}
                            }}

                            // 초기 로드
                            {(string.IsNullOrEmpty(script)
                                ? "console.log('No initial data');"
                                : $"updateChartData({script});")}
                        </script>
                    </body>
                    </html>
                    ";
                                return html;
        }
        #endregion

        #region Remove Old Loaded Logic
        // 이전 VisionWeek_Loaded, _viewModel.LoadVisionNgDataWeekAsync() 등 제거/주석
        // => Parent에서 SetData(...)를 호출하기 때문
        #endregion
    }
}
