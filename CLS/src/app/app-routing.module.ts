import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { DataImportsComponent } from './dataimports/dataimports.component';
import { NotFoundComponent } from './errors/not-found/not-found.component';
import { ServerErrorComponent } from './errors/server-error/server-error.component';
import { TestErrorComponent } from './errors/test-error/test-error.component';
import { HomeComponent } from './home/home.component';
import { AuthGuard } from './_guards/auth.guard';
import { UserListComponent } from './users/user-list/user-list.component';

const routes: Routes = [
  {path: '',  loadChildren: () => import('./auth/auth.module').then(m => m.AuthModule)},
  {path: 'users', component: UserListComponent},
  {path: 'measuredata', loadChildren: () => import('./measure-data/measure-data.module').then(m => m.MeasureDataModule)},
  {path: 'targets', loadChildren: () => import('./target/target.module').then(m => m.TargetModule)},
  {path: 'measures', loadChildren: () => import('./measure/measure.module').then(m => m.MeasureModule)},
  {path: 'measuredefinition', loadChildren: () => import('./measure-definition/measure-definition.module').then(m => m.MeasureDefinitionModule)},
  {path: 'hierarchy', loadChildren: () => import('./hierarchy/hierarchy.module').then(m => m.HierarchyModule)},
  {path: 'dataimports', component: DataImportsComponent},
  {path: '', 
    runGuardsAndResolvers: 'always', 
    canActivate: [AuthGuard], children: [
      {path: 'home', component: HomeComponent},
      // {path: 'users', component: UserListComponent},
      // {path: 'members', component: MemberListComponent},
      // {path: 'members/:id', component: MemberDetailComponent},
      // {path: 'lists', component: ListsComponent},
      // {path: 'messages', component: MessagesComponent},
    ]
  },

  //{path: 'targets', component: TargetsComponent},
  //{path: 'measures', component: MeasuresComponent},
  //{path: 'measuredefinition', component: MeasureDefinitionComponent},
  //{path: 'hierarchy', component: HierarchyComponent},
  //{path: 'dataimports', component: DataImportsComponent},
  {path: 'errors', component: TestErrorComponent},
  {path: 'not-found', component: NotFoundComponent},
  {path: 'server-error', component: ServerErrorComponent},
  {path: '**', component: NotFoundComponent, pathMatch: 'full'}
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
