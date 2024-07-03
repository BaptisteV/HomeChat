const perfChartId = "perf-chart";
const lineChartContainerId = "line-chart";

const eventSourcePerf = new EventSource("api/PerformanceSummary?interval=250", {
        withCredentials: false,
});

const performanceSummaryHistory = [];

function onNewPerf(event) {
    const data = JSON.parse(event.data);
    performanceSummaryHistory.push(data);
    updatePerfGraph(data);
}

eventSourcePerf.addEventListener("performanceSummary", onNewPerf);

eventSourcePerf.onerror = (e) => {
    console.error("Error in performance eventSource", e);
};

const barChart = new CanvasJS.Chart(perfChartId, {
    responsive: true,
    axisY: {
        title: "% used",
        includeZero: true,
        suffix: " %",
        minimum: 0,
        maximum: 100,
    },
    data: [{
        type: "column",
        yValueFormatString: "#,## %",
        indexLabel: "{y}",
        dataPoints: [
            { label: "cpu", y: 50 },
            { label: "gpu", y: 50 },
            { label: "ram", y: 50 },
        ]
    }]
});

function updatePerfGraph(perfSummary) {
    let dps = barChart.options.data[0].dataPoints;

    const conf = ["Cpu", "Gpu", "Ram"];
    for (let i = 0; i < conf.length; i++) {
        dps[i].y = perfSummary[conf[i]].PercentUsed;
        dps[i].color = "#DDDDDD";
        dps[i].label = conf[i].toUpperCase();
    }
    barChart.options.data[0].dataPoints = dps;
    barChart.render();
}

function showCpuLineGraph() {
    // cancel previous event source
    // init event source for component
    // update graph on event source message
}

const perfChartDiv = document.getElementById(perfChartId);
perfChartDiv.onclick = onBarGraphClick;
perfChartDiv.style.cursor = "pointer";

function onBarGraphClick(e) {
    const x = e.pageX - e.currentTarget.getBoundingClientRect().left;
    const contentStartX = 60;
    let contentX = x - contentStartX;
    
    const contentWidth = e.currentTarget.getBoundingClientRect().width - contentStartX;
    const zone = contentX / contentWidth;
    console.log(`clicked in zone ${zone}`);

    if (zone < 0)
        return;
        
    if (zone < 0.33) {
        // CPU
        updateLineChart([1, 2, 33, 33, 36, 30, 44, 23, 12, 4], "Cpu");
    } else if (zone > 0.66) {
        // RAM
        updateLineChart([22, 2, 31, 32, 89, 78, 100, 100, 99, 97], "Ram");
    } else {
        // GPU
        updateLineChart([1, 2, 3, 2, 12, 10, 4, 18, 19, 10], "Gpu");
    }
}

const lineChartContainer = document.getElementById(lineChartContainerId);
console.log("adding linechart to ", lineChartContainer);

const lineChart = new CanvasJS.Chart(lineChartContainerId, {
    animationEnabled: true,
    theme: "light2",
    title: {
        text: " "
    },
    axisY: {
        title: "% used",
        includeZero: true,
        suffix: "%",
        minimum: 0,
        maximum: 100
    },
    data: [{
        type: "line",
        dataPoints: [
            { x: 0, y: 0 },
            { x: 1, y: 0 },
            { x: 2, y: 0 },
            { x: 3, y: 0 },
            { x: 4, y: 0 },
            { x: 5, y: 0 },
            { x: 6, y: 0 },
            { x: 7, y: 0 },
            { x: 8, y: 0 },
            { x: 9, y: 0 },
        ],
    }]
});

lineChart.render();

function updateLineChart(data, name) {
    console.log(`updating chart for ${name}`, data);
    lineChart.options.title.text = `${name} Usage`;
    lineChart.options.data[0].name = name;
    lineChart.options.data[0].dataPoints = data.map((value, index) => ({ x: index, y: value }));
    lineChart.render();
}

// Dirty hack
setTimeout(() => { window.dispatchEvent(new Event('resize')); }, 300);