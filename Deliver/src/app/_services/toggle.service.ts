import { Injectable, signal } from "@angular/core"

@Injectable({
  providedIn: "root"
})
export class ToggleService {
  readonly toggle = signal(true)

  constructor() { }
}
