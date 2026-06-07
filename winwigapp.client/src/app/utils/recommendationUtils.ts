// Utility functions for generating buy/sell/wait recommendations based on technical indicators

export type Recommendation = "BUY" | "SELL" | "WAIT";

export interface TechnicalData {
  rsi?: number[];
  macd?: { value: number; signal: number; histogram: number }[];
  sma50?: number[];
  sma200?: number[];
  currentPrice?: number;
}

export interface StrategyConfig {
  rsiLow: number;
  rsiHigh: number;
  macdBuy: boolean;
  sma50Above200: boolean;
}

/**
 * Calculates a recommendation based on technical indicators
 * For Dashboard: returns BUY or WAIT
 * For Portfolio: returns BUY, SELL, or WAIT
 * Can optionally use strategy parameters for more precise recommendations
 */
export function getStockRecommendation(
  technicalData: TechnicalData,
  forPortfolio: boolean = false,
  strategy?: StrategyConfig
): Recommendation {
  const rsi = technicalData.rsi?.[technicalData.rsi.length - 1];
  const macd = technicalData.macd?.[technicalData.macd.length - 1];
  const sma50 = technicalData.sma50?.[technicalData.sma50.length - 1];
  const sma200 = technicalData.sma200?.[technicalData.sma200.length - 1];

  // If strategy is provided, use strategy-based logic
  if (strategy) {
    return getStrategyBasedRecommendation(
      rsi,
      macd,
      sma50,
      sma200,
      strategy,
      forPortfolio
    );
  }

  // Otherwise use default technical analysis
  return getDefaultRecommendation(rsi, macd, sma50, sma200, forPortfolio);
}

/**
 * Get recommendation based on strategy parameters
 * Mirrors the backend logic from StrategyExecutionService
 */
function getStrategyBasedRecommendation(
  rsi: number | undefined,
  macd: { value: number; signal: number; histogram: number } | undefined,
  sma50: number | undefined,
  sma200: number | undefined,
  strategy: StrategyConfig,
  forPortfolio: boolean
): Recommendation {
  // Check BUY signal conditions
  const buySignal = checkBuySignal(rsi, macd, sma50, sma200, strategy);

  // Check SELL signal conditions (only for portfolio)
  const sellSignal = forPortfolio ? checkSellSignal(rsi, macd, sma50, sma200, strategy) : false;

  if (sellSignal) {
    return "SELL";
  }

  if (buySignal) {
    return "BUY";
  }

  return "WAIT";
}

/**
 * Get default recommendation based on standard technical analysis
 */
function getDefaultRecommendation(
  rsi: number | undefined,
  macd: { value: number; signal: number; histogram: number } | undefined,
  sma50: number | undefined,
  sma200: number | undefined,
  forPortfolio: boolean
): Recommendation {
  let buySignals = 0;
  let sellSignals = 0;

  // RSI Analysis (0-100 scale)
  // RSI < 30 = oversold (potential buy)
  // RSI > 70 = overbought (potential sell)
  if (rsi !== undefined) {
    if (rsi < 30) {
      buySignals++;
    } else if (rsi > 70) {
      sellSignals++;
    }
  }

  // MACD Analysis
  // Positive histogram = bullish
  // Negative histogram = bearish
  // Crossing above signal = buy signal
  // Crossing below signal = sell signal
  if (macd !== undefined) {
    if (macd.histogram > 0 && macd.value > macd.signal) {
      buySignals++;
    } else if (macd.histogram < 0 && macd.value < macd.signal) {
      sellSignals++;
    }
  }

  // SMA Analysis
  // Price above SMA50 and SMA50 above SMA200 = uptrend (buy signal)
  // Price below SMA50 and SMA50 below SMA200 = downtrend (sell signal)
  if (sma50 !== undefined && sma200 !== undefined) {
    if (sma50 > sma200) {
      buySignals++;
    } else if (sma50 < sma200) {
      sellSignals++;
    }
  }

  // Determine recommendation based on signal count
  if (forPortfolio) {
    // Portfolio: can recommend to SELL
    if (sellSignals > buySignals && sellSignals >= 2) {
      return "SELL";
    }
  }

  // Common logic for both
  if (buySignals >= 2) {
    return "BUY";
  }

  return "WAIT";
}

/**
 * Check BUY signal based on strategy conditions
 * Mirrors CheckBuySignal from StrategyExecutionService
 */
function checkBuySignal(
  rsi: number | undefined,
  macd: { value: number; signal: number; histogram: number } | undefined,
  sma50: number | undefined,
  sma200: number | undefined,
  strategy: StrategyConfig
): boolean {
  let buyConditions = true;

  // Sprawdź RSI - powinno być poniżej RsiLow (przeceniona spółka - sygnał do kupna)
  if (rsi !== undefined && rsi > strategy.rsiLow) {
    buyConditions = false;
  }

  // Sprawdź MACD - jeśli strategia wymaga MACD buy sygnału
  if (strategy.macdBuy && macd !== undefined && macd.histogram <= 0) {
    buyConditions = false;
  }

  // Sprawdź SMA - jeśli strategia wymaga, aby SMA50 było powyżej SMA200
  if (strategy.sma50Above200 && sma50 !== undefined && sma200 !== undefined && sma50 < sma200) {
    buyConditions = false;
  }

  return buyConditions;
}

/**
 * Check SELL signal based on strategy conditions
 * Mirrors CheckSellSignal from StrategyExecutionService
 */
function checkSellSignal(
  rsi: number | undefined,
  macd: { value: number; signal: number; histogram: number } | undefined,
  sma50: number | undefined,
  sma200: number | undefined,
  strategy: StrategyConfig
): boolean {
  let sellConditions = true;

  // Sprawdź RSI - powinno być powyżej RsiHigh (wykupiona spółka - sygnał do sprzedaży)
  if (rsi !== undefined && rsi < strategy.rsiHigh) {
    sellConditions = false;
  }

  // Sprawdź MACD - jeśli sygnał się zmienił
  if (strategy.macdBuy && macd !== undefined && macd.histogram <= 0) {
    sellConditions = false;
  }

  // Sprawdź SMA - jeśli SMA50 spadło poniżej SMA200
  if (strategy.sma50Above200 && sma50 !== undefined && sma200 !== undefined && sma50 < sma200) {
    sellConditions = false;
  }

  return sellConditions;
}

/**
 * Get recommendation color for UI display
 */
export function getRecommendationColor(recommendation: Recommendation): string {
  switch (recommendation) {
    case "BUY":
      return "text-green-500 bg-green-500/10 border-green-500/20";
    case "SELL":
      return "text-red-500 bg-red-500/10 border-red-500/20";
    case "WAIT":
      return "text-yellow-500 bg-yellow-500/10 border-yellow-500/20";
    default:
      return "text-gray-500 bg-gray-500/10 border-gray-500/20";
  }
}

/**
 * Get recommendation text for UI display
 */
export function getRecommendationText(recommendation: Recommendation): string {
  switch (recommendation) {
    case "BUY":
      return "Kup";
    case "SELL":
      return "Sprzedaj";
    case "WAIT":
      return "Czekaj";
    default:
      return "Brak danych";
  }
}
