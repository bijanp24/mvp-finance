import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface OptionParams {
  S: number;
  K: number;
  T: number;
  r: number;
  sigma: number;
  option_type: string;
}

export interface Greeks {
  delta: number;
  gamma: number;
  theta: number;
  vega: number;
  rho: number;
}

export interface PayoffPoint {
  underlying: number;
  payoff: number;
}

@Injectable({
  providedIn: 'root'
})
export class OptionsService {
  private http = inject(HttpClient);
  // Assuming Python API runs on port 8000
  private pythonApiUrl = 'http://localhost:8000/api';

  getGreeks(params: OptionParams): Observable<Greeks> {
    return this.http.post<Greeks>(`${this.pythonApiUrl}/calc/greeks`, params);
  }

  getPayoff(params: OptionParams): Observable<PayoffPoint[]> {
    return this.http.post<PayoffPoint[]>(`${this.pythonApiUrl}/viz/payoff`, params);
  }
}
