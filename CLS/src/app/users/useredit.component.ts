import { SelectionModel } from "@angular/cdk/collections"
import { FlatTreeControl } from "@angular/cdk/tree"
import { Component, OnInit } from "@angular/core"
import { MatTreeFlatDataSource, MatTreeFlattener } from "@angular/material/tree"
import { ActivatedRoute } from "@angular/router"
import { RegionFilter } from "../_models/regionfilter"
import { UserRole } from "../_models/user"
import { LoggerService } from "../_services/logger.service"
import { UserService } from "../_services/user.service"

export class RegionFlatNode {
    hierarchy!: string
    level!: number
    expandable!: boolean
}

@Component({
    selector: "app-useredit",
    templateUrl: "./useredit.component.html",
    styleUrls: ["./useredit.component.scss"]
})
export class UserEditComponent implements OnInit {
    title = "Edit User"
    roles!: UserRole[]
    disabledAll = false
    flatNodeMap = new Map<RegionFlatNode, RegionFilter>()
    nestedNodeMap = new Map<RegionFilter, RegionFlatNode>()
    treeControl: FlatTreeControl<RegionFlatNode>
    treeFlattener!: MatTreeFlattener<RegionFilter, RegionFlatNode>
    treeData!: MatTreeFlatDataSource<RegionFilter, RegionFlatNode>
    model = {
        userName: "",
        firstName: "",
        lastName: "",
        roleId: 1,
        department: "",
        active: false,
        checklistSelection: new SelectionModel<RegionFlatNode>(true)
    }
    
    constructor(private route: ActivatedRoute, private userService: UserService, private logger: LoggerService) {
        this.treeFlattener = new MatTreeFlattener(
            this.transformer,
            this.getLevel,
            this.isExpandable,
            this.getChildren
        )
        this.treeControl = new FlatTreeControl<RegionFlatNode>(this.getLevel, this.isExpandable)
        this.treeData = new MatTreeFlatDataSource(this.treeControl, this.treeFlattener)
        this.treeData.data = []
    }

    ngOnInit() {
        this.route.paramMap.subscribe(params => {
            let id = Number(params.get("id"))
            this.userService.getUser(id).subscribe(value => {
                let user = value.data[0]
                this.roles = value.roles

                let hierarchy = value.hierarchy
                this.treeData.data = hierarchy
                let v = [hierarchy[0]], n = 0
                while (n < v.length) {  // flatten hierarchy into v
                    if (Array.isArray(v[n].sub) && v[n].sub.length > 0) {
                        v = v.concat(...v[n].sub)
                    }

                    n++
                }

                let m = new Map<number, RegionFilter>(v.map(rf => [rf.id, rf]))
                let initiallySelectedValues = user.hierarchiesId
                    .map(id => m.get(id)).filter((rf): rf is RegionFilter => !!rf)
                    .map(rf => this.nestedNodeMap.get(rf)).filter((fn): fn is RegionFlatNode => !!fn)
                this.model = {
                    userName: user.userName,
                    roleId: user.roleId,
                    firstName: user.firstName ?? "",
                    lastName: user.lastName ?? "",
                    department: user.department ?? "",
                    active: user.active === "true",
                    checklistSelection: new SelectionModel<RegionFlatNode>(true, initiallySelectedValues)
                }
            })
        })
    }

    getLevel = (node: RegionFlatNode): number => node.level
    isExpandable = (node: RegionFlatNode): boolean => node.expandable
    getChildren = (node: RegionFilter): RegionFilter[] => node.sub
    hasChild = (_: number, _nodeData: RegionFlatNode): boolean => _nodeData.expandable
    hasNoContent = (_: number, _nodeData: RegionFlatNode): boolean => _nodeData.hierarchy === ""
    transformer = (node: RegionFilter, level: number): RegionFlatNode => {
        const prevNode = this.nestedNodeMap.get(node)
        const flatNode = prevNode?.hierarchy === node.hierarchy ? prevNode : new RegionFlatNode()
        flatNode.hierarchy = node.hierarchy
        flatNode.level = level
        flatNode.expandable = !!node.sub?.length
        this.flatNodeMap.set(flatNode, node)
        this.nestedNodeMap.set(node, flatNode)
        return flatNode
    }

    descendantsAllSelected(node: RegionFlatNode): boolean {
        const descendants = this.treeControl.getDescendants(node)
        const descAllSelected =
            descendants.length > 0 &&
            descendants.every(child => {
                return this.model.checklistSelection.isSelected(child)
            })
        return descAllSelected
    }

    descendantsPartiallySelected(node: RegionFlatNode): boolean {
        const descendants = this.treeControl.getDescendants(node)
        const result = descendants.some(child => this.model.checklistSelection.isSelected(child))
        return result && !this.descendantsAllSelected(node)
    }

    getParentNode(node: RegionFlatNode): RegionFlatNode | undefined {
        const currentLevel = this.getLevel(node)
        if (currentLevel < 1) {
            return
        }

        const startIndex = this.treeControl.dataNodes.indexOf(node) - 1
        for (let i = startIndex; i >= 0; i--) {
            const currentNode = this.treeControl.dataNodes[i]

            if (this.getLevel(currentNode) < currentLevel) {
                return currentNode
            }
        }

        return
    }

    updateRootNodeSelection(node: RegionFlatNode): void {
        const nodeSelected = this.model.checklistSelection.isSelected(node)
        const descendants = this.treeControl.getDescendants(node)
        const descAllSelected =
            descendants.length > 0 &&
            descendants.every(child => {
                return this.model.checklistSelection.isSelected(child)
            })
        if (nodeSelected && !descAllSelected) {
            this.model.checklistSelection.deselect(node)
        }
        else if (!nodeSelected && descAllSelected) {
            this.model.checklistSelection.select(node)
        }
    }

    checkAllParentsSelection(node: RegionFlatNode): void {
        let parent: RegionFlatNode | undefined = this.getParentNode(node)
        while (parent) {
            this.updateRootNodeSelection(parent)
            parent = this.getParentNode(parent)
        }
    }

    regionFilterSelectionToggle(node: RegionFlatNode): void {
        this.model.checklistSelection.toggle(node)
        const descendants = this.treeControl.getDescendants(node)
        if (this.model.checklistSelection.isSelected(node)) {
            this.model.checklistSelection.select(...descendants)
        }
        else {
            this.model.checklistSelection.deselect(...descendants)
        }

        // Force update for the parent
        descendants.forEach(child => this.model.checklistSelection.isSelected(child))
        this.checkAllParentsSelection(node)
    }

    save() {
        this.logger.logWarning("Save user not implemented")
        console.log({
            ...this.model,
            checklistSelection: this.model.checklistSelection.selected.map(it => this.flatNodeMap.get(it)?.id)
        })
    }
}
