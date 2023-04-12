import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ToggleService {
  private toggle = new Subject<boolean>();
  toggle$ = this.toggle.asObservable();

  constructor() { }

  setToggle(toggle: boolean) {
    this.toggle.next(toggle);
  }
}
