import { useState, useMemo, useEffect } from "react";
import { useParams, useNavigate, Link } from "react-router";
import { StockResponse, getCandlestickData, getTechnicalIndicators, getStocks } from "../../utils/stocksApi";
import { authFetch } from "../../utils/authHelper";
import {
    ArrowLeft,
    TrendingUp,
    TrendingDown,
    Activity,
    BarChart3,
    ShoppingCart,
} from "lucide-react";
import {
    ComposedChart,
    Bar,
    Line,
    XAxis,
    YAxis,
    CartesianGrid,
    Tooltip,
    ResponsiveContainer,
    LineChart,
    Shape,
    Cell,
    Scatter,
    ScatterChart,
    ReferenceLine,
    Customized,
    Area,
} from "recharts";
import { BuyModal } from "./BuyModal";

type TimeInterval = "1D" | "1W" | "1M" | "3M" | "1Y";

// Custom Candlestick Chart Component
const CandlestickChart = ({ data }: { data: any[] }) => {
    const [hoveredIndex, setHoveredIndex] = useState<number | null>(null);

    if (!data || data.length === 0) {
        return <div className="text-gray-400">Brak danych do wyświetlenia</div>;
    }

    const minPrice = Math.min(...data.map(d => Math.min(d.low, d.open, d.close, d.high)));
    const maxPrice = Math.max(...data.map(d => Math.max(d.high, d.open, d.close, d.low)));

    return (
        <ResponsiveContainer width="100%" height={350}>
            <ComposedChart
                data={data}
                margin={{ top: 20, right: 30, left: 60, bottom: 20 }}
            >
                <CartesianGrid strokeDasharray="3 3" stroke="#374151" />
                <XAxis dataKey="date" stroke="#9ca3af" />
                <YAxis
                    stroke="#9ca3af"
                    domain={[minPrice - 5, maxPrice + 5]}
                    tickFormatter={formatPriceAxis}
                />
                <Tooltip
                    contentStyle={{
                        backgroundColor: "#1f2937",
                        border: "1px solid #374151",
                        borderRadius: "0.5rem",
                        color: "#fff",
                    }}
                    content={({ active, payload }) => {
                        if (active && payload && payload[0]) {
                            const dataItem = payload[0].payload;
                            return (
                                <div className="bg-gray-800 border border-gray-600 rounded p-3 text-sm">
                                    <p className="text-gray-300">{dataItem.date}</p>
                                    <p className="text-green-400">O: {dataItem.open.toFixed(2)} PLN</p>
                                    <p className="text-green-400">H: {dataItem.high.toFixed(2)} PLN</p>
                                    <p className="text-red-400">L: {dataItem.low.toFixed(2)} PLN</p>
                                    <p className="text-white">C: {dataItem.close.toFixed(2)} PLN</p>
                                </div>
                            );
                        }
                        return null;
                    }}
                    cursor={{ fill: 'rgba(255, 255, 255, 0.1)' }}
                />
                {/* Invisible bars for tooltip interaction */}
                <Bar
                    dataKey="close"
                    fill="transparent"
                    stroke="transparent"
                    isAnimationActive={false}
                />
                {/* Render candlesticks using Customized component */}
                <Customized
                    component={<CandlestickRenderer data={data} />}
                />
            </ComposedChart>
        </ResponsiveContainer>
    );
};

// Candlestick renderer component using Customized
const CandlestickRenderer = (props: any) => {
    const { data, xAxisMap, yAxisMap } = props;

    if (!data || !xAxisMap || !yAxisMap) {
        return null;
    }

    const xAxis = Object.values(xAxisMap)[0] as any;
    const yAxis = Object.values(yAxisMap)[0] as any;

    if (!xAxis || !yAxis) {
        return null;
    }

    return (
        <g>
            {data.map((item: any, index: number) => {
                const { open, close, high, low, date } = item;

                try {
                    const xPos = xAxis.scale(date) !== undefined
                        ? xAxis.scale(date) + (xAxis.bandwidth ? xAxis.bandwidth() / 2 : 0)
                        : xAxis.scale(index);

                    const yHigh = yAxis.scale(high);
                    const yLow = yAxis.scale(low);
                    const yOpen = yAxis.scale(open);
                    const yClose = yAxis.scale(close);

                    const isUp = close >= open;
                    const color = isUp ? "#10b981" : "#ef4444";

                    const candleWidth = 6;
                    const candleX = xPos - candleWidth / 2;

                    return (
                        <g key={`candle-${index}`}>
                            {/* Wick line */}
                            <line
                                x1={xPos}
                                y1={yHigh}
                                x2={xPos}
                                y2={yLow}
                                stroke={color}
                                strokeWidth={1}
                            />
                            {/* Body */}
                            <rect
                                x={candleX}
                                y={Math.min(yOpen, yClose)}
                                width={candleWidth}
                                height={Math.max(Math.abs(yClose - yOpen), 2)}
                                fill={color}
                                stroke={color}
                                strokeWidth={1}
                            />
                        </g>
                    );
                } catch (e) {
                    return null;
                }
            })}
        </g>
    );
};

// Formatter functions for axes
const formatPriceAxis = (value: number) => {
    return `${value.toFixed(2)} PLN`;
};

const formatVolumeAxis = (value: number) => {
    if (value >= 1000000) {
        return `${(value / 1000000).toFixed(1)}M`;
    } else if (value >= 1000) {
        return `${(value / 1000).toFixed(1)}K`;
    }
    return value.toString();
};



export function StockDetails() {
    const { symbol } = useParams<{ symbol: string }>();
    const navigate = useNavigate();
    const [selectedInterval, setSelectedInterval] = useState<TimeInterval>("1M");
    const [showBuyModal, setShowBuyModal] = useState(false);
    const [activeTab, setActiveTab] = useState<"chart" | "technical">("chart");

    const [stock, setStock] = useState<StockResponse | null>(null);
    const [candleData, setCandleData] = useState<any[]>([]);
    const [technicalIndicators, setTechnicalIndicators] = useState<any | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        let mounted = true;
        const loadStock = async () => {
            try {
                const data = await getStocks();
                if (!mounted) return;
                const found = data.find((s) => s.symbol === symbol);
                setStock(found ?? null);
            } catch (err) {
                console.error(err);
                setError(err instanceof Error ? err.message : String(err));
            }
        };

        loadStock();
        return () => { mounted = false; };
    }, [symbol]);

    useEffect(() => {
        if (!stock) return;
        let mounted = true;
        const loadData = async () => {
            setLoading(true);
            setError(null);
            try {
                const days = selectedInterval === "1D" ? 1 : selectedInterval === "1W" ? 7 : selectedInterval === "1M" ? 30 : selectedInterval === "3M" ? 90 : 252;

                const candles = await getCandlestickData(stock.symbol, days);
                const tech = await getTechnicalIndicators(stock.symbol, days);

                if (!mounted) return;
                setCandleData(candles);
                setTechnicalIndicators(tech);
            } catch (err) {
                console.error(err);
                setError(err instanceof Error ? err.message : String(err));
            } finally {
                if (mounted) setLoading(false);
            }
        };

        loadData();
        return () => { mounted = false; };
    }, [stock, selectedInterval]);

    const chartData = useMemo(() => {
        return candleData.map((candle, index) => {
            const timestamp = new Date(candle.timestamp);
            let date: string;

            if (selectedInterval === "1D") {
                // Dla 1D wyświetl godziny
                date = timestamp.toLocaleString("pl-PL", {
                    hour: "2-digit",
                    minute: "2-digit",
                });
            } else {
                // Dla innych interwałów wyświetl datę
                date = timestamp.toLocaleDateString("pl-PL", {
                    day: "2-digit",
                    month: "2-digit",
                });
            }

            return {
                date,
                price: candle.close,
                high: candle.high,
                low: candle.low,
                open: candle.open,
                close: candle.close,
                volume: candle.volume,
                candleColor: candle.close >= candle.open ? "#10b981" : "#ef4444",
            };
        });
    }, [candleData, selectedInterval]);

    const rsiData = useMemo(() => {
        if (!technicalIndicators) return [];
        return candleData.map((candle, index) => {
            const timestamp = new Date(candle.timestamp);
            let date: string;

            if (selectedInterval === "1D") {
                date = timestamp.toLocaleString("pl-PL", {
                    hour: "2-digit",
                    minute: "2-digit",
                });
            } else {
                date = timestamp.toLocaleDateString("pl-PL", {
                    day: "2-digit",
                    month: "2-digit",
                });
            }

            return {
                date,
                rsi: technicalIndicators.rsi[index],
            };
        });
    }, [candleData, technicalIndicators, selectedInterval]);

    const macdData = useMemo(() => {
        if (!technicalIndicators) return [];
        return candleData.map((candle, index) => {
            const timestamp = new Date(candle.timestamp);
            let date: string;

            if (selectedInterval === "1D") {
                date = timestamp.toLocaleString("pl-PL", {
                    hour: "2-digit",
                    minute: "2-digit",
                });
            } else {
                date = timestamp.toLocaleDateString("pl-PL", {
                    day: "2-digit",
                    month: "2-digit",
                });
            }

            return {
                date,
                macd: technicalIndicators.macd[index]?.value || 0,
                signal: technicalIndicators.macd[index]?.signal || 0,
                histogram: technicalIndicators.macd[index]?.histogram || 0,
            };
        });
    }, [candleData, technicalIndicators, selectedInterval]);

    if (!stock) {
        return (
            <div className="text-center py-12">
                <p className="text-gray-400 mb-4">Nie znaleziono spółki</p>
                <Link to="/" className="text-emerald-500 hover:text-emerald-400">
                    Wróć do listy
                </Link>
            </div>
        );
    }

    const intervals: TimeInterval[] = ["1D", "1W", "1M", "3M", "1Y"];

    return (
        <div className="space-y-6">
            <div>
                <button
                    onClick={() => navigate("/")}
                    className="flex items-center gap-2 text-gray-400 hover:text-white mb-4 transition-colors"
                >
                    <ArrowLeft className="w-4 h-4" />
                    Powrót do listy
                </button>

                <div className="flex items-start justify-between">
                    <div>
                        <div className="flex items-center gap-3 mb-2">
                            <h1 className="text-3xl font-bold text-white">{stock.symbol}</h1>
                            <span
                                className={`px-3 py-1 rounded-full text-sm font-medium ${stock.changePercent >= 0
                                        ? "bg-emerald-500/10 text-emerald-500"
                                        : "bg-red-500/10 text-red-500"
                                    }`}
                            >
                                {stock.changePercent >= 0 ? "+" : ""}
                                {stock.changePercent.toFixed(2)}%
                            </span>
                        </div>
                        <p className="text-gray-400">{stock.name}</p>
                    </div>

                    <button
                        onClick={() => setShowBuyModal(true)}
                        className="flex items-center gap-2 px-6 py-3 bg-emerald-500 hover:bg-emerald-600 text-white rounded-lg transition-colors font-medium"
                    >
                        <ShoppingCart className="w-5 h-5" />
                        Kup / Sprzedaj
                    </button>
                </div>
            </div>

            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                <div className="bg-gray-900 rounded-lg p-4 border border-gray-800">
                    <div className="text-gray-400 text-sm mb-1">Aktualny kurs</div>
                    <div className="text-2xl font-bold text-white">
                        {stock.currentPrice.toLocaleString("pl-PL", {
                            minimumFractionDigits: 2,
                            maximumFractionDigits: 2,
                        })}{" "}
                        PLN
                    </div>
                    <div
                        className={`flex items-center gap-1 mt-2 text-sm ${stock.changePercent >= 0 ? "text-emerald-500" : "text-red-500"
                            }`}
                    >
                        {stock.changePercent >= 0 ? (
                            <TrendingUp className="w-4 h-4" />
                        ) : (
                            <TrendingDown className="w-4 h-4" />
                        )}
                        {stock.change >= 0 ? "+" : ""}
                        {stock.change.toFixed(2)} PLN
                    </div>
                </div>

                <div className="bg-gray-900 rounded-lg p-4 border border-gray-800">
                    <div className="text-gray-400 text-sm mb-1">Wolumen</div>
                    <div className="text-xl font-bold text-white">
                        {stock.volume.toLocaleString("pl-PL")}
                    </div>
                </div>

                <div className="bg-gray-900 rounded-lg p-4 border border-gray-800">
                    <div className="text-gray-400 text-sm mb-1">P/E</div>
                    <div className="text-xl font-bold text-white">{(stock.peRatio || 0).toFixed(2)}</div>
                </div>

                <div className="bg-gray-900 rounded-lg p-4 border border-gray-800">
                    <div className="text-gray-400 text-sm mb-1">ROE</div>
                    <div className="text-xl font-bold text-white">{(stock.roe || 0).toFixed(2)}%</div>
                </div>
            </div>

            <div className="bg-gray-900 rounded-lg p-6 border border-gray-800">
                <div className="flex items-center justify-between mb-6">
                    <div className="flex gap-2">
                        <button
                            onClick={() => setActiveTab("chart")}
                            className={`flex items-center gap-2 px-4 py-2 rounded-lg transition-colors ${activeTab === "chart"
                                    ? "bg-emerald-500 text-white"
                                    : "bg-gray-800 text-gray-400 hover:text-white"
                                }`}
                        >
                            <BarChart3 className="w-4 h-4" />
                            Wykres świecowy
                        </button>
                        <button
                            onClick={() => setActiveTab("technical")}
                            className={`flex items-center gap-2 px-4 py-2 rounded-lg transition-colors ${activeTab === "technical"
                                    ? "bg-emerald-500 text-white"
                                    : "bg-gray-800 text-gray-400 hover:text-white"
                                }`}
                        >
                            <Activity className="w-4 h-4" />
                            Analiza techniczna
                        </button>
                    </div>

                    <div className="flex gap-2">
                        {intervals.map((interval) => (
                            <button
                                key={interval}
                                onClick={() => setSelectedInterval(interval)}
                                className={`px-3 py-1.5 rounded text-sm font-medium transition-colors ${selectedInterval === interval
                                        ? "bg-emerald-500 text-white"
                                        : "bg-gray-800 text-gray-400 hover:text-white"
                                    }`}
                            >
                                {interval}
                            </button>
                        ))}
                    </div>
                </div>

                {activeTab === "chart" && (
                    <div className="space-y-6">
                        <div>
                            <h3 className="text-white font-medium mb-4">Wykres świecowy</h3>
                            <CandlestickChart data={chartData} />
                        </div>

                        <div>
                            <h3 className="text-white font-medium mb-4">Wykres ceny (Cena zamknięcia)</h3>
                            <ResponsiveContainer width="100%" height={350}>
                                <ComposedChart data={chartData}>
                                    <CartesianGrid strokeDasharray="3 3" stroke="#374151" />
                                    <XAxis dataKey="date" stroke="#9ca3af" />
                                    <YAxis stroke="#9ca3af" domain={["dataMin - 5", "dataMax + 5"]} tickFormatter={formatPriceAxis} />
                                    <Tooltip
                                        contentStyle={{
                                            backgroundColor: "#1f2937",
                                            border: "1px solid #374151",
                                            borderRadius: "0.5rem",
                                            color: "#fff",
                                        }}
                                        content={({ active, payload }) => {
                                            if (active && payload && payload[0]) {
                                                const data = payload[0].payload;
                                                return (
                                                    <div className="bg-gray-800 border border-gray-600 rounded p-3 text-sm">
                                                        <p className="text-gray-300">{data.date}</p>
                                                        <p className="text-white">Cena: {data.price.toFixed(2)} PLN</p>
                                                    </div>
                                                );
                                            }
                                            return null;
                                        }}
                                    />
                                    <Line
                                        type="monotone"
                                        dataKey="price"
                                        stroke="#10b981"
                                        strokeWidth={2}
                                        dot={false}
                                    />
                                </ComposedChart>
                            </ResponsiveContainer>
                        </div>

                        <div>
                            <h3 className="text-white font-medium mb-4">Wolumen</h3>
                            <ResponsiveContainer width="100%" height={150}>
                                <ComposedChart data={chartData}>
                                    <CartesianGrid strokeDasharray="3 3" stroke="#374151" />
                                    <XAxis dataKey="date" stroke="#9ca3af" />
                                    <YAxis stroke="#9ca3af" tickFormatter={formatVolumeAxis} />
                                    <Tooltip
                                        contentStyle={{
                                            backgroundColor: "#1f2937",
                                            border: "1px solid #374151",
                                            borderRadius: "0.5rem",
                                            color: "#fff",
                                        }}
                                    />
                                    <Bar dataKey="volume" fill="#3b82f6" />
                                </ComposedChart>
                            </ResponsiveContainer>
                        </div>
                    </div>
                )}

                {activeTab === "technical" && technicalIndicators && (
                    <div className="space-y-6">
                        <div>
                            <h3 className="text-white font-medium mb-4">
                                RSI (Relative Strength Index)
                            </h3>
                            <ResponsiveContainer width="100%" height={200}>
                                <LineChart data={rsiData}>
                                    <CartesianGrid strokeDasharray="3 3" stroke="#374151" />
                                    <XAxis dataKey="date" stroke="#9ca3af" />
                                    <YAxis stroke="#9ca3af" domain={[0, 100]} />
                                    <Tooltip
                                        contentStyle={{
                                            backgroundColor: "#1f2937",
                                            border: "1px solid #374151",
                                            borderRadius: "0.5rem",
                                            color: "#fff",
                                        }}
                                    />
                                    <Line type="monotone" dataKey="rsi" stroke="#8b5cf6" strokeWidth={2} />
                                    <Line
                                        type="monotone"
                                        dataKey={() => 70}
                                        stroke="#ef4444"
                                        strokeDasharray="5 5"
                                        strokeWidth={1}
                                    />
                                    <Line
                                        type="monotone"
                                        dataKey={() => 30}
                                        stroke="#10b981"
                                        strokeDasharray="5 5"
                                        strokeWidth={1}
                                    />
                                </LineChart>
                            </ResponsiveContainer>
                            <div className="mt-2 text-sm text-gray-400">
                                Aktualna wartość RSI:{" "}
                                <span className="text-white font-medium">
                                    {rsiData[rsiData.length - 1]?.rsi ? rsiData[rsiData.length - 1].rsi.toFixed(2) : "N/A"}
                                </span>
                            </div>
                        </div>

                        <div>
                            <h3 className="text-white font-medium mb-4">MACD</h3>
                            <ResponsiveContainer width="100%" height={200}>
                                <ComposedChart data={macdData}>
                                    <CartesianGrid strokeDasharray="3 3" stroke="#374151" />
                                    <XAxis dataKey="date" stroke="#9ca3af" />
                                    <YAxis stroke="#9ca3af" tickFormatter={(value) => value.toFixed(2)} />
                                    <Tooltip
                                        contentStyle={{
                                            backgroundColor: "#1f2937",
                                            border: "1px solid #374151",
                                            borderRadius: "0.5rem",
                                            color: "#fff",
                                        }}
                                    />
                                    <Bar dataKey="histogram" fill="#6366f1" />
                                    <Line type="monotone" dataKey="macd" stroke="#10b981" strokeWidth={2} />
                                    <Line
                                        type="monotone"
                                        dataKey="signal"
                                        stroke="#f59e0b"
                                        strokeWidth={2}
                                    />
                                </ComposedChart>
                            </ResponsiveContainer>
                        </div>

                        <div>
                            <h3 className="text-white font-medium mb-4">Średnie kroczące (SMA)</h3>
                            <ResponsiveContainer width="100%" height={200}>
                                <LineChart data={chartData.map((d, i) => ({
                                    ...d,
                                    sma50: technicalIndicators.sma50[i],
                                    sma200: technicalIndicators.sma200[i],
                                }))}>
                                    <CartesianGrid strokeDasharray="3 3" stroke="#374151" />
                                    <XAxis dataKey="date" stroke="#9ca3af" />
                                    <YAxis stroke="#9ca3af" tickFormatter={formatPriceAxis} />
                                    <Tooltip
                                        contentStyle={{
                                            backgroundColor: "#1f2937",
                                            border: "1px solid #374151",
                                            borderRadius: "0.5rem",
                                            color: "#fff",
                                        }}
                                    />
                                    <Line
                                        type="monotone"
                                        dataKey="price"
                                        stroke="#10b981"
                                        strokeWidth={2}
                                        name="Cena"
                                    />
                                    <Line
                                        type="monotone"
                                        dataKey="sma50"
                                        stroke="#3b82f6"
                                        strokeWidth={2}
                                        name="SMA 50"
                                    />
                                    <Line
                                        type="monotone"
                                        dataKey="sma200"
                                        stroke="#f59e0b"
                                        strokeWidth={2}
                                        name="SMA 200"
                                    />
                                </LineChart>
                            </ResponsiveContainer>
                        </div>
                    </div>
                )}
            </div>

            {showBuyModal && (
                <BuyModal stock={stock} onClose={() => setShowBuyModal(false)} />
            )}
        </div>
    );
}