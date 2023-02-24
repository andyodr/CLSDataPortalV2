import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-errors',
    template: `
<div class="panel panel-default ng-hide noBorder marB0" [hidden]="!showError">
  <div class="panel-body padB0 marB0 noBorder">
    <ngb-alert [ngModel]="alert" type="danger" (closed)="closeError()" *ngIf="showContentPage">    
      <p>{{errorMsg.heading}}</p>
      <p *ngIf="error.id != null">{{error.id}}</p>
      <p *ngIf="error.status != null">{{error.status}}</p>
      <p>{{error.message}}</p>    
    </ngb-alert>
    
    <ngb-alert [ngModel]="alert" type="danger" *ngIf="!showContentPage">    
      <p *ngIf="error.id != null">{{error.id}}</p>
      <p *ngIf="error.status != null">{{error.status}}</p>
      <p>{{error.message}}</p>    
    </ngb-alert>
  </div>      
</div>`
})
export class ErrorsComponent {
    @Input() error!: { id: any, status: any, message: any }
}
