import React, { Component } from 'react';
import { bindActionCreators } from 'redux';
import { connect } from 'react-redux';
import { Link } from 'react-router-dom';
import { actionCreators } from '../store/WikiWalks';
import Head from './Helmet';

class Category extends Component {

    constructor(props) {
        super(props);

        this.state = {
            pages: [],
        }
    }

    componentDidMount() {
        const getData = async () => {
            const url = `api/WikiWalks/getAllWords`;
            const response = await fetch(url);
            const pages = await response.json();
            this.setState({ pages });
        }
        getData();
    }

    render() {
        const { pages } = this.state;
        const description = `This is a list of keywords from Wikipedia! Choose a keyword you are interested in!`;
        const arrDesc = description.split("! ");
        const lineChangeDesc = arrDesc.map((d, i) => <span key={i}>{d}{i < arrDesc.length - 1 && ". "}<br /></span>);
        return (
            <div>
                <Head
                    title={"All Keywords"}
                    desc={description}
                />
                <div className="breadcrumbs" itemScope itemType="https://schema.org/BreadcrumbList" style={{ textAlign: "left" }}>
                    <span itemProp="itemListElement" itemScope itemType="http://schema.org/ListItem">
                        <Link to="/" itemProp="item" style={{ marginRight: "5px", marginLeft: "5px" }}>
                            <span itemProp="name">
                                {"Home"}
                            </span>
                        </Link>
                        <meta itemProp="position" content="1" />
                    </span>
                    {" > "}
                    <span itemProp="itemListElement" itemScope itemType="http://schema.org/ListItem">
                        <span itemProp="name" style={{ marginRight: "5px", marginLeft: "5px" }}>
                            {"All Keywords"}
                        </span>
                        <meta itemProp="position" content="2" />
                    </span>
                </div>
                <hr />
                <h1>Keywords</h1>
                <br />
                {lineChangeDesc}
                <br />
                <table className='table table-striped'>
                    <thead>
                        <tr>
                            <th>Keywords</th>
                            <th>Found Articles</th>
                        </tr>
                    </thead>
                    <tbody>
                        {pages.length > 0 ? pages.filter(page => page.referenceCount > 4).map(page =>
                            <tr key={page.wordId}>
                                <td>
                                    <Link to={"/word/" + page.wordId}>{page.word}</Link>
                                </td>
                                <td>
                                    {page.referenceCount} pages
                                </td>
                            </tr>
                        )
                            :
                            <tr><td>Loading...</td><td></td></tr>}
                    </tbody>
                </table>
            </div>
        );
    }
}

export default connect(
    state => state.wikiWalks,
    dispatch => bindActionCreators(actionCreators, dispatch)
)(Category);
