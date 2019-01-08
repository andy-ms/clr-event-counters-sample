namespace Rickshaw {
    class Graph {
        constructor(options: GraphOptions);
        render(): void;
    }

    interface GraphOptions {
        readonly element: Element;
        readonly renderer?: "area" | "stack" | "bar" | "line" | "scatterplot";
        readonly min?: number | "auto";
        readonly series: ReadonlyArray<Series>;
    }

    interface Series {
        readonly name: string;
        readonly color: string;
        readonly data: DataPoint[];
    }

    interface DataPoint {
        readonly x: number;
        readonly y: number;
    }

    namespace Graph {
        class Legend {
            constructor(options: {
                readonly graph: Graph,
                readonly element: Element,
            });
        }

        namespace Axis {
            class Time {
                constructor(options: { readonly graph: Graph, readonly ticksTreatment?: string, readonly timeFixture?: Fixtures.Time.Local });
                render(): void;
            }

            class Y {
                constructor(options: { readonly graph: Graph, readonly tickFormat?: unknown });
                render(): void;
            }
        }
    }

    namespace Fixtures {
        namespace Number {
            const formatKMBT: unknown;
        }

        namespace Time {
            class Local {
            }
        }
    }
}
