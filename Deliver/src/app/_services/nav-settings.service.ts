import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class NavSettingsService {
  enableNavBar = true;
  
  constructor() { }

  hideNavBar() { 
    this.enableNavBar = false;
  }

  showNavBar() {
    this.enableNavBar = true;
  }
}
