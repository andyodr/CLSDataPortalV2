import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ToggleService {
  private toggle = new Subject<any>();
  toggle$ = this.toggle.asObservable();

  constructor() { }

  setToggle(toggle: any) {
    this.toggle.next(toggle);
  }
}
