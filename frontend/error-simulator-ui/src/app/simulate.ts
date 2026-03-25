import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class SimulateService {

  api = 'https://localhost:7290/api/simulate';

  constructor(private http: HttpClient) { }

  cpu() {
    return this.http.post(`${this.api}/cpu`, {});
  }

  memory() {
    return this.http.post(`${this.api}/memory`, {});
  }

  slow() {
    return this.http.post(`${this.api}/slow`, {});
  }

  db() {
    return this.http.post(`${this.api}/db`, {});
  }

  exception() {
    return this.http.post(`${this.api}/exception`, {});
  }

  stats() {
    return this.http.get(`${this.api}/stats`);
  }
}