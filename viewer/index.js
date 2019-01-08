// @ts-check
/// <reference path="./smoothie.d.ts" />
/// <reference path="./rickshaw.d.ts" />

class Chart_smoothie {
	/** @param {Element} element */
	constructor(element) {
		this.series = new TimeSeries();
		const canvas = document.createElement("canvas");
		element.appendChild(canvas);
		//    <canvas id="chart" width="600" height="400"></canvas>
		this.chart = new SmoothieChart();
		this.chart.addTimeSeries(this.series, { strokeStyle: 'rgba(0, 255, 0, 1)' });
		this.chart.streamTo(canvas, 500);

		function resize() { // in case element size changes, must change canvas too
			canvas.width = element.clientWidth;
			canvas.height = element.clientHeight;
		};
		resize();
		window.onresize = resize;
	}

	/**
	 * @param {number} timeInMilleseconds
	 * @param {number} y
	 * @return {void}
	 */
	update(timeInMilleseconds, y) {
		this.series.append(timeInMilleseconds, y);
	}
}

class Chart_rickshaw {
	/** @param {Element} element */
	constructor(element) {
		/** @type {Array<Rickshaw.DataPoint>} */
		this.data = [];

		const graph = new Rickshaw.Graph({
			element,
			renderer: "line",
			min: "auto",
			series: [
				{ name: "serires_name", color: "steelblue", data: this.data },
			],
		});
		this.graph = graph;

		new Rickshaw.Graph.Legend({ graph, element });

		this.xAxis = new Rickshaw.Graph.Axis.Time({
			graph,
			//timeFixture: new Rickshaw.Fixtures.Time.Local()
		});

		new Rickshaw.Graph.Axis.Y({ graph });
	}

	/**
	 * @param {number} timeInMilleseconds
	 * @param {number} y
	 */
	update(timeInMilleseconds, y) {
		this.data.push({ x: timeInMilleseconds, y });
		//TODO:better
		const max_len = 200;
		if (this.data.length > max_len)
			this.data.shift();
		console.log(this.data.length);

		this.graph.render();
	}
}

/**
 * @param {boolean} b
 * @return {void}
 */
function assert(b) {
	if (!b) throw new Error();
}

window.onload = () => {
	const div = document.querySelector('#graph');
	const chart = new Chart_rickshaw(div);

	const socket = new WebSocket("ws://localhost:8002"); // Calling the constructor opens the connection
	socket.onopen = event => {
		console.log("socket open");
	};
	socket.onmessage = event => {
		const { timeMs, y } = JSON.parse(event.data);
		assert(typeof timeMs === "number" && typeof y === "number");
		chart.update(timeMs, y);
	};
	socket.onerror = error => {
		console.log(error);
		console.log("An error occurred");
	};
	socket.onclose = event => {
		console.log(event);
		console.log("socket closed");
	};
};

