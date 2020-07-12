import React, { Component } from 'react';
import { bindActionCreators } from 'redux';
import { connect } from 'react-redux';
import { Button } from 'reactstrap';
import { Link } from 'react-router-dom';
import { actionCreators } from '../store/WikiWalks';
import Head from './Helmet';

class Top extends Component {

    constructor(props) {
        super(props);

        this.state = {
            categories: [],
        }
    }

    componentDidMount() {
        const getData = async () => {
            const url = `api/WikiWalks/getAllCategories`;
            const response = await fetch(url);
            const categories = await response.json();
            this.setState({ categories: categories.slice(0, 100) });
            setTimeout(() => this.setState({ categories }), 1000);
        }
        getData();
    }

    render() {
        const { categories } = this.state;
        return (
            <div>
                <Head
                    title={"Wiki Ninja"}
                    desc={"This website introduces you to articles of Wikipedia for each category!"}
                />
                <h1>Welcome to Wiki Ninja!</h1>
                <br />
                <p>
                    Do you know Wikipedia? It is the best online dictionary in the world!<br />
                    This website introduces you to articles of Wikipedia for each category!
                </p>
                <br />
                <table className='table table-striped'>
                    <thead>
                        <tr>
                            <th>Category Name</th>
                            <th><span style={{
                                display: "inline-block",
                                minWidth: 100,
                            }}>Number of Keywords</span></th>
                        </tr>
                    </thead>
                    <tbody>
                        {categories.length > 0 ? categories.map(category =>
                            <tr key={category.category}>
                                <td>
                                    {<Link to={"/category/" + encodeURIComponent(category.category.split(" ").join("_"))}>{category.category}</Link>}
                                </td>
                                <td>
                                    {category.cnt} keywords
                                </td>
                            </tr>
                        )
                            :
                            <tr><td>Loading...</td><td></td></tr>}
                    </tbody>
                </table>
                <hr />
                <Link to="/all">
                    <center>
                        <Button><b>{"Check all keywords"}</b></Button>
                    </center>
                </Link>
                <br />
            </div>
        );
    }
}

export default connect(
    state => state.wikiWalks,
    dispatch => bindActionCreators(actionCreators, dispatch)
)(Top);
