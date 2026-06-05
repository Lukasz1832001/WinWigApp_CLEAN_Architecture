const API_BASE_URL = '/api/strategies';

export interface CreateStrategyRequest {
  name: string;
  targetReturn: number;
  investmentHorizon: number;
  rsiLow: number;
  rsiHigh: number;
  macdBuy: boolean;
  sma50Above200: boolean;
}

export interface StrategyResponse {
  id: string;
  name: string;
  targetReturn: number;
  investmentHorizon: number;
  rsiLow: number;
  rsiHigh: number;
  macdBuy: boolean;
  sma50Above200: boolean;
  isActive: boolean;
  createdAt: string;
}

export interface BaseResponse {
  success: boolean;
  message: string;
}

export interface ToggleStrategyResponse extends BaseResponse {
  isActive: boolean;
}

const getAuthToken = () => localStorage.getItem('token');

const getHeaders = () => ({
  'Content-Type': 'application/json',
  'Authorization': `Bearer ${getAuthToken()}`,
});

export const strategiesApi = {
  async createStrategy(request: CreateStrategyRequest): Promise<StrategyResponse> {
    const response = await fetch(`${API_BASE_URL}`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Błąd podczas tworzenia strategii');
    }

    return response.json();
  },

  async getStrategies(): Promise<StrategyResponse[]> {
    const response = await fetch(`${API_BASE_URL}`, {
      headers: getHeaders(),
    });

    if (!response.ok) {
      throw new Error('Błąd podczas pobierania strategii');
    }

    return response.json();
  },

  async getStrategy(id: string): Promise<StrategyResponse> {
    const response = await fetch(`${API_BASE_URL}/${id}`, {
      headers: getHeaders(),
    });

    if (!response.ok) {
      throw new Error('Strategia nie znaleziona');
    }

    return response.json();
  },

  async updateStrategy(id: string, request: CreateStrategyRequest): Promise<BaseResponse> {
    const response = await fetch(`${API_BASE_URL}/${id}`, {
      method: 'PUT',
      headers: getHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Błąd podczas aktualizacji strategii');
    }

    return response.json();
  },

  async deleteStrategy(id: string): Promise<BaseResponse> {
    const response = await fetch(`${API_BASE_URL}/${id}`, {
      method: 'DELETE',
      headers: getHeaders(),
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Błąd podczas usuwania strategii');
    }

    return response.json();
  },

  async toggleStrategy(id: string): Promise<ToggleStrategyResponse> {
    const response = await fetch(`${API_BASE_URL}/${id}/toggle`, {
      method: 'PUT',
      headers: getHeaders(),
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Błąd podczas przełączania strategii');
    }

    return response.json();
  },
};
