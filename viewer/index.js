// @ts-check
/// <reference path="./smoothie.d.ts" />

class Chart {
	constructor() {
		this.start = Date.now();

		this.series = new TimeSeries();
		const canvas = (/** @type {HTMLCanvasElement} */ (document.getElementById('chart')));
		this.chart = new SmoothieChart();
		this.chart.addTimeSeries(this.series, { strokeStyle: 'rgba(0, 255, 0, 1)' });
		this.chart.streamTo(canvas, 500);
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

/**
 * @param {boolean} b
 * @return {void}
 */
function assert(b) {
	if (!b) throw new Error();
}

window.onload = () => {
	const chart = new Chart();

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

