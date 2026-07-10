from fastapi import FastAPI
from pydantic import BaseModel
import numpy as np
import pandas as pd
import scipy.stats as si

app = FastAPI(title="Options Engine API")

class OptionParams(BaseModel):
    S: float  # Underlying price
    K: float  # Strike price
    T: float  # Time to maturity (years)
    r: float  # Risk-free interest rate
    sigma: float  # Volatility
    option_type: str = "call" # "call" or "put"

@app.get("/")
def read_root():
    return {"status": "Options Engine Running"}

@app.post("/api/calc/greeks")
def calc_greeks(params: OptionParams):
    # Simple Black-Scholes Greeks calculation placeholder
    S = params.S
    K = params.K
    T = params.T
    r = params.r
    sigma = params.sigma
    
    # Calculate d1 and d2
    if T <= 0 or sigma <= 0:
        return { "error": "T and sigma must be greater than zero." }

    d1 = (np.log(S / K) + (r + 0.5 * sigma ** 2) * T) / (sigma * np.sqrt(T))
    d2 = (np.log(S / K) + (r - 0.5 * sigma ** 2) * T) / (sigma * np.sqrt(T))
    
    if params.option_type.lower() == 'call':
        delta = si.norm.cdf(d1, 0.0, 1.0)
    else:
        delta = -si.norm.cdf(-d1, 0.0, 1.0)
        
    gamma = si.norm.pdf(d1, 0.0, 1.0) / (S * sigma * np.sqrt(T))
    vega = S * si.norm.pdf(d1, 0.0, 1.0) * np.sqrt(T)
    
    return {
        "delta": float(delta),
        "gamma": float(gamma),
        "theta": 0.0, # placeholder
        "vega": float(vega),
        "rho": 0.0 # placeholder
    }

@app.post("/api/viz/payoff")
def viz_payoff(params: OptionParams):
    # Generate payoff data
    S_range = np.linspace(params.S * 0.5, params.S * 1.5, 100)
    if params.option_type.lower() == 'call':
        payoff = np.maximum(S_range - params.K, 0)
    else:
        payoff = np.maximum(params.K - S_range, 0)
        
    df = pd.DataFrame({"underlying": S_range, "payoff": payoff})
    return df.to_dict(orient="records")
