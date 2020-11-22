import React, { Component } from "react";
import { connect } from "react-redux";
import { Link } from "react-router-dom";
import { Button } from "reactstrap";
import { bindActionCreators } from "redux";
import { actionCreators } from "../store/WikiWalks";
import Head from "./Helmet";

class Top extends Component {
    unmounted = false;

    constructor(props) {
        super(props);

        this.state = {
            categories: [],
        };
    }

    componentDidMount() {
        const getData = async () => {
            let previousCount = 0;
            let i = 100;
            while (true) {
                const url = `api/WikiWalks/getPartialCategories?num=${i}`;
                const response = await fetch(url);
                const categories = await response.json();

                if (this.unmounted || previousCount === categories.length) {
                    break;
                }

                this.setState({ categories });
                await new Promise(resolve => setTimeout(() => resolve(), 100));

                i = i + 10;
                previousCount = categories.length;
            }
        };
        getData();
    }

    componentWillUnmount() {
        this.unmounted = true;
    }

    render() {
        const { categories } = this.state;
        return (
            <div>
                <Head
                    title={"Wiki Ninja"}
                    desc={
                        "This website introduces you to articles of Wikipedia for each category!"
                    }
                    noad
                />
                <h1>Welcome to Wiki Ninja!</h1>
                <br />
                <p>
                    Do you know Wikipedia? It is the best online dictionary in
                    the world!
                    <br />
                    This website introduces you to articles of Wikipedia for
                    each category!
                </p>
                <br />
                <table className="table table-striped">
                    <thead>
                        <tr>
                            <th style={{ verticalAlign: "middle" }}>
                                Category Name
                            </th>
                            <th>
                                <span
                                    style={{
                                        display: "inline-block",
                                        minWidth: 100,
                                    }}
                                >
                                    Number of Keywords
                                </span>
                            </th>
                        </tr>
                    </thead>
                    <tbody>
                        {categories.length > 0 ? (
                            categories.map(category => (
                                <tr key={category.category}>
                                    <td>
                                        {
                                            <Link
                                                to={
                                                    "/category/" +
                                                    encodeURIComponent(
                                                        category.category
                                                            .split(" ")
                                                            .join("_")
                                                    )
                                                }
                                            >
                                                {category.category}
                                            </Link>
                                        }
                                    </td>
                                    <td>{category.cnt} keywords</td>
                                </tr>
                            ))
                        ) : (
                            <tr>
                                <td>Loading...</td>
                                <td></td>
                            </tr>
                        )}
                    </tbody>
                </table>
                {categories.length > 0 && categories.length < 200 && (
                    <center>Loading...</center>
                )}
                <hr />
                <Link to="/all">
                    <center>
                        <Button>
                            <b>{"Check all keywords"}</b>
                        </Button>
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
