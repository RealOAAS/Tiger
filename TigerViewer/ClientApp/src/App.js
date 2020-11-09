import React, { Component } from 'react';
import { Route } from 'react-router';
import './custom.css'

export default class App extends Component {
  constructor(props){
    super(props)
    this.state = {master_package_dict : {}, entry_table : [], selected_package : "", selected_package_index: -1}
  }
  
  static displayName = App.name;

  async componentDidMount(){
    const master_packages_request = await fetch("MasterPackagesDict")
    const master_packages = await master_packages_request.json()
    this.setState({master_package_dict: master_packages})
  }

  changeSelectedPackage = (index) => {
    this.setState({
      selected_package_index: index,
      selected_package: this.state.master_package_dict[Object.keys(this.state.master_package_dict)[index].toString()]
    })

    this.updateEntryTable(this.state.master_package_dict[Object.keys(this.state.master_package_dict)[index].toString()]);
  }

  updateEntryTable = async (packageName) => {
    const request = await fetch("PackageEntries", {headers: {package_name: packageName}});
    const data = await request.json();
    this.setState({entry_table: data});
  }

  int_to_hex_string = (number, length=4) => {
    return ("0".repeat(length) + number.toString(16)).substr(-length).toUpperCase()
  }

  render () {
    const verticalPadding = 50;
    const horizontalPadding = 50;

    return (
      <div style={{paddingLeft: horizontalPadding, paddingRight: horizontalPadding, paddingTop: verticalPadding, paddingBlock: verticalPadding, display: 'flex', flexDirection: 'row'}}>
        <div style={{height: "90vh"}}> {/* The div for the Packages List */}
          <h4>Packages List</h4>
          <div style={{overflow: 'auto', padding: 10, height: '90vh'}}>
            {Object.keys(this.state.master_package_dict).map((value, index) => 
              <div key={index} onClick={() => this.changeSelectedPackage(index)} style={{backgroundColor: index == this.state.selected_package_index ? '#147EFB' : 'transparent', color: index == this.state.selected_package_index ? 'white' : 'black', paddingLeft: 5, paddingRight: 5, borderRadius: 5}}>
                <p style={{padding:0, margin:0}}>{this.state.master_package_dict[value.toString()]}</p>
              </div>
              )}
          </div>
        </div>
        <div style={{height: "90vh"}}> {/* The div for the entry table */}
          <h4>Entry Table: {this.state.selected_package}</h4>
          <div style={{overflow: 'auto', padding: 10, height: '90vh'}}>
            <table style={{width: 900}} className="table">
              <tr>
                <th>ID</th>
                <th>Reference ID</th>
                <th>Reference Package ID</th>
                <th>Reference Package Name</th>
                <th>File Size</th>
                <th>Type</th>
                <th>Subtype</th>
              </tr>
              
              {this.state.entry_table.map((entry, index) => 
                <tr style={{backgroundColor: index % 2 == 0 ? 'white' : 'lightgray'}}>
                  <td>{this.int_to_hex_string(index)}</td>
                  <td>{this.int_to_hex_string(entry.reference_id)}</td>
                  <td>{this.int_to_hex_string(entry.reference_package_id)}</td>
                  <td>{this.state.master_package_dict[entry.reference_package_id.toString()]}</td>
                  <td>{entry.file_size}</td>
                  <td>{entry.type == 8 || entry.type == 16 ? this.int_to_hex_string(entry.entry_a, 8) : entry.type}</td>
                  <td>{entry.subtype}</td>
                </tr>
                )}
            </table>
          </div>
        </div>
      </div>
    );
  }
}
