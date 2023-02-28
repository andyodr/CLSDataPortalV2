import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-errors',
    template: `
<div class="panel panel-default ng-hide noBorder marB0">
  <div class="panel-body padB0 marB0 noBorder">
    <ngb-alert type="danger" (closed)="closeError()" *ngIf="showContentPage">    
      <p>{{error.heading}}</p>
      <p *ngIf="error.id != null">{{error.id}}</p>
      <p *ngIf="error.status != null">{{error.status}}</p>
      <p>{{error.message}}</p>    
    </ngb-alert>
    
    <ngb-alert type="danger" *ngIf="!showContentPage">    
      <p *ngIf="error.id != null">{{error.id}}</p>
      <p *ngIf="error.status != null">{{error.status}}</p>
      <p>{{error.message}}</p>    
    </ngb-alert>
  </div>      
</div>`
})
export class ErrorsComponent {
    @Input() error!: { id: any, status: any, message: any, heading: any }
    @Input() showContentPage!: boolean
    @Input() closeError!: () => void
}
