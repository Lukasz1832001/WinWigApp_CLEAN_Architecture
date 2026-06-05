// API service for fetching stocks data from backend
export interface StockResponse {
  symbol: string;
  name: string;
  currentPrice: number;
  volume: number;
  openPrice: number;
  closePrice: number;
  peRatio: number;
  pbRatio: number;
  roe: number;
  change: number;
  changePercent: number;
}

export interface CandlestickData {
  timestamp: number;
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
}

export interface TechnicalIndicators {
  rsi: number[];
  macd: { value: number; signal: number; histogram: number }[];
  sma50: number[];
  sma200: number[];
}

export const getStocks = async (): Promise<StockResponse[]> => {
  try {
    const response = await fetch('/api/stocks');

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    const data = await response.json();
    return data;
  } catch (error) {
    console.error('Error fetching stocks:', error);
    throw error;
  }
};

export const getCandlestickData = async (symbol: string, days: number = 90) => {
  try {
    const response = await fetch(`/api/stocks/${symbol}/candlestick?days=${days}`);

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    const data = await response.json();
    return data;
  } catch (error) {
    console.error(`Error fetching candlestick data for ${symbol}:`, error);
    throw error;
  }
};

export const getTechnicalIndicators = async (symbol: string, days: number = 90) => {
  try {
    const response = await fetch(`/api/stocks/${symbol}/technical?days=${days}`);

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    const data = await response.json();
    return data;
  } catch (error) {
    console.error(`Error fetching technical indicators for ${symbol}:`, error);
    throw error;
  }
};
