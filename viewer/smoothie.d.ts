declare class TimeSeries {
    append(currentTimeMs: number, y: number): void;
}

declare class SmoothieChart {
    addTimeSeries(series: TimeSeries, options: { readonly strokeStyle: string }): void;
    streamTo(canvas: HTMLCanvasElement, interval: number): void;
}
