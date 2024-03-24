import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { MeasureDataComponent } from './measure-data/measure-data.component';
import { DataImportsComponent } from './dataimports/dataimports.component';
import { NotFoundComponent } from './errors/not-found/not-found.component';
import { ServerErrorComponent } from './errors/server-error/server-error.component';
import { TestErrorComponent } from './errors/test-error/test-error.component';
import { AuthGuard } from './_guards/auth.guard';

const title = "DELIVER - "

export const routes: Routes = [
    { path: '', loadChildren: () => import('./auth/auth.module').then(m => m.AuthModule) },
    { path: 'measuredata', title: `${ title }Measure Data`, component: MeasureDataComponent },
    {
        path: "users", children: [
            { path: "add", title: `${ title }Add User`,
            loadComponent: () => import("./users/useradd.component").then(m => m.UserAddComponent) },
            { path: "", title: `${ title }Users`,
            loadComponent: () => import("./users/userlist.component").then(m => m.UserListComponent) },
            { path: ":id", title: `${ title }Edit User`,
            loadComponent: () => import("./users/useredit.component").then(m => m.UserEditComponent) }
        ]
    },
    {
        path: "settings", title: `${ title }Settings`,
        loadComponent: () => import("./calendar/settings.component").then(m => m.CalendarSettingsComponent)
    },
    {
        path: "hierarchy", title: `${ title }Region Hierarchy`,
        loadComponent: () => import("./hierarchy/hierarchy.component").then(m => m.RegionHierarchyComponent) },
    {
        path: '',
        runGuardsAndResolvers: 'always',
        canActivate: [AuthGuard], children: []
    },
    {
        path: 'targets', title: `${ title }Targets`,
        loadComponent: () => import("./targets/targets.component").then(m => m.TargetsComponent) },
    {
        path: 'measures', title: `${ title }Measures`,
        loadComponent: () => import("./measures/measures.component").then(m => m.MeasuresComponent)
    },
    {
        path: "measuredefinition", children: [
            {
                path: "", title: `${ title }Measure Definitions`,
                loadComponent: () => import("./measuredefinition/measuredefinition.component").then(m => m.MeasureDefinitionComponent)
            },
            {
                path: "add", title: `${ title }Add Measure Definition`,
                loadComponent: () => import("./measuredefinition/measuredefinition-edit.component").then(m => m.MeasureDefinitionEditComponent)
            },
            {
                path: ":id", title: `${ title }Edit Measure Definition`,
                loadComponent: () => import("./measuredefinition/measuredefinition-edit.component").then(m => m.MeasureDefinitionEditComponent)
            },
        ]
    },
    { path: "dataimports", title: `${ title }Data Imports`, component: DataImportsComponent },
    { path: 'errors', component: TestErrorComponent },
    { path: 'not-found', component: NotFoundComponent },
    { path: 'server-error', component: ServerErrorComponent },
    { path: '**', component: NotFoundComponent, pathMatch: 'full' }
];

@NgModule({
    imports: [RouterModule.forRoot(routes)],
    exports: [RouterModule]
})
export class AppRoutingModule { }
